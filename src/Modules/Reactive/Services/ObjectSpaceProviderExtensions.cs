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
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.AppDomainExtensions;
using Xpand.Extensions.XAF.ObjectSpaceProviderExtensions;

namespace Xpand.XAF.Modules.Reactive.Services{
    public static class ObjectSpaceProviderExtensions{
        public static IObservable<TResult> NewObjectSpace<TResult>(this IObjectSpaceProvider provider,Func<IObjectSpace, IObservable<TResult>> factory) 
            => provider == null ? Observable.Empty<TResult>() : Observable.Using(provider.CreateObjectSpace, factory);

        private static readonly ISubject<IObjectSpaceProvider> SchemaUpdatingSubject=Subject.Synchronize(new Subject<IObjectSpaceProvider>());
        private static readonly ISubject<IObjectSpaceProvider> SchemaUpdatedSubject=Subject.Synchronize(new Subject<IObjectSpaceProvider>());
        private static readonly ISubject<IObjectSpace> ObjectSpaceCreatedSubject=Subject.Synchronize(new Subject<IObjectSpace>());

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
                var methodInfo = provider.GetType().Methods(name).First(info => !info.Parameters().Any());
                methodInfo.PatchWith( postFix: new HarmonyMethod(typeof(ObjectSpaceProviderExtensions), nameof(CreateObjectSpace)));
            }
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static void CreateObjectSpace(IObjectSpace __result) 
            => ObjectSpaceCreatedSubject.OnNext(__result);

        private static void PatchSchemaUpdated(this XafApplication application) 
            => application.ObjectSpaceProviders.Where(provider => provider is not NonPersistentObjectSpaceProvider)
                .ForEach(PatchSchemaUpdated);

        public static void PatchSchemaUpdated(this IObjectSpaceProvider provider) {
            if (provider.IsMiddleTier()) return;
            var name = $"{typeof(IObjectSpaceProvider).FullName}.{nameof(IObjectSpaceProvider.UpdateSchema)}";
            provider.GetType().Methods(nameof(IObjectSpaceProvider.UpdateSchema),name)
                .Last(info => info.DeclaringType is { IsAbstract: false })
                .PatchWith(new HarmonyMethod(GetMethodInfo(nameof(SchemaUpdating))))
                .PatchWith(postFix: new HarmonyMethod(GetMethodInfo(nameof(SchemaUpdated))));
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
            => SchemaUpdatingSubject.AsObservable().Cast<TProvider>();

        public static IObservable<TProvider> WhenSchemaUpdated<TProvider>(this TProvider provider) where TProvider:IObjectSpaceProvider 
            => SchemaUpdatedSubject.AsObservable().Cast<TProvider>();
        
        public static IObservable<IObjectSpace> WhenObjectSpaceCreated<TProvider>(this TProvider provider) where TProvider:IObjectSpaceProvider 
            => ObjectSpaceCreatedSubject.AsObservable();

    }
}