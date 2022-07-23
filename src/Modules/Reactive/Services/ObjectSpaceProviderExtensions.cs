using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using DevExpress.ExpressApp;
using Fasterflect;
using HarmonyLib;
using Xpand.Extensions.Harmony;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.Harmony;
using Xpand.Extensions.XAF.ObjectSpaceProviderExtensions;

namespace Xpand.XAF.Modules.Reactive.Services{
    public static class ObjectSpaceProviderExtensions{
        public static IObservable<TResult> NewObjectSpace<TResult>(this IObjectSpaceProvider provider,Func<IObjectSpace, IObservable<TResult>> factory) 
            => provider == null ? Observable.Empty<TResult>() : Observable.Using(provider.CreateObjectSpace, factory);

        private static readonly ISubject<IObjectSpaceProvider> SchemaUpdatingSubject=Subject.Synchronize(new Subject<IObjectSpaceProvider>());
        private static readonly ISubject<IObjectSpaceProvider> SchemaUpdatedSubject=Subject.Synchronize(new Subject<IObjectSpaceProvider>());
        private static readonly ISubject<(IObjectSpace objectSpace,IObjectSpaceProvider objectSpaceProvider)> ObjectSpaceCreatedSubject=Subject.Synchronize(new Subject<(IObjectSpace objectSpace,IObjectSpaceProvider objectSpaceProvider)>());

        private static MethodInfo GetMethodInfo(string methodName) 
            => typeof(ObjectSpaceProviderExtensions).GetMethods(BindingFlags.Static|BindingFlags.NonPublic).First(info => info.Name == methodName);

        internal static IObservable<Unit> PatchObjectSpaceProvider(this XafApplication application) 
            => application.WhenSetupComplete()
                .Do(_ => {
                    application.PatchSchemaUpdated();
                    application.PatchObjectSpaceCreated();
                })
                .ToUnit();

        private static void PatchObjectSpaceCreated(this XafApplication application) {
            var name = nameof(IObjectSpaceProvider.CreateObjectSpace);
            foreach (var provider in application.ObjectSpaceProviders) {
                new HarmonyMethod(typeof(ObjectSpaceProviderExtensions), nameof(CreateObjectSpace))
                    .PostFix(provider.GetType().Methods(name).First(info => !info.Parameters().Any()),true);
            }
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static void CreateObjectSpace(IObjectSpaceProvider __instance,IObjectSpace __result) 
            => ObjectSpaceCreatedSubject.OnNext((__result, __instance));

        private static void PatchSchemaUpdated(this XafApplication application) 
            => application.ObjectSpaceProviders.Where(provider => provider is not NonPersistentObjectSpaceProvider)
                .ForEach(PatchSchemaUpdated);

        public static void PatchSchemaUpdated(this IObjectSpaceProvider provider) {
            if (provider.IsMiddleTier()) return;
            var name = $"{typeof(IObjectSpaceProvider).FullName}.{nameof(IObjectSpaceProvider.UpdateSchema)}";
            var methodInfo = provider.GetType().Methods(nameof(IObjectSpaceProvider.UpdateSchema),name)
                .Last(info => info.DeclaringType is { IsAbstract: false });
            methodInfo.PatchWith(new HarmonyMethod(GetMethodInfo(nameof(SchemaUpdating))),new HarmonyMethod(GetMethodInfo(nameof(SchemaUpdated))));
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static bool SchemaUpdating(IObjectSpaceProvider __instance){
            SchemaUpdatingSubject.OnNext(__instance);
            return true;
        }
        
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static void SchemaUpdated(IObjectSpaceProvider __instance) 
            => SchemaUpdatedSubject.OnNext(__instance);

        public static IObservable<TProvider> WhenSchemaUpdating<TProvider>(this TProvider provider) where TProvider:IObjectSpaceProvider 
            => SchemaUpdatingSubject.AsObservable().Where(spaceProvider => spaceProvider==(IObjectSpaceProvider)provider).Cast<TProvider>();

        public static IObservable<TProvider> WhenSchemaUpdated<TProvider>(this TProvider provider) where TProvider:IObjectSpaceProvider 
            => SchemaUpdatedSubject.AsObservable().Where(spaceProvider => spaceProvider==(IObjectSpaceProvider)provider).Cast<TProvider>();
        
        public static IObservable<IObjectSpace> WhenObjectSpaceCreated<TProvider>(this TProvider provider) where TProvider:IObjectSpaceProvider 
            => ObjectSpaceCreatedSubject.AsObservable().Where(t=>t.objectSpaceProvider==(IObjectSpaceProvider)provider).Select(t => t.objectSpace);

    }
}