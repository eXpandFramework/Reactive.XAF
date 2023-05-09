using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Security;
using DevExpress.Xpo.DB;
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

        private static readonly ISubject<(IObjectSpace objectSpace, IObjectSpaceProvider objectSpaceProvider,bool updating)>
            ObjectSpaceCreatedSubject = Subject.Synchronize(new Subject<(IObjectSpace objectSpace, IObjectSpaceProvider objectSpaceProvider,bool updating)>());

        private static MethodInfo GetMethodInfo(string methodName) 
            => typeof(ObjectSpaceProviderExtensions).GetMethods(BindingFlags.Static|BindingFlags.NonPublic).First(info => info.Name == methodName);

        internal static IObservable<Unit> PatchObjectSpaceProvider(this XafApplication application) 
            => application.WhenSetupComplete()
                .Do(_ => {
                    application.PatchSchemaUpdated();
                    application.PatchObjectSpaceCreated();
                    application.PatchPopulateAdditionalObjectSpaces();
                    application.PatchNonSecuredObjectSpaceCreated();
                    application.PatchUpdatingObjectSpaceCreated();
                })
                .ToUnit();

        private static void PatchPopulateAdditionalObjectSpaces(this XafApplication application) 
            => application.PatchProviderObjectSpaceCreated<IObjectSpaceProvider>(nameof(IObjectSpaceProvider.CreateObjectSpace), _ => true, nameof(CreateObjectSpace));

        private static void PatchObjectSpaceCreated(this XafApplication application) 
            => application.PatchProviderObjectSpaceCreated<IObjectSpaceProvider>( nameof(IObjectSpaceProvider.CreateObjectSpace),_ => true,nameof(CreateObjectSpace));
        
        private static void PatchNonSecuredObjectSpaceCreated(this XafApplication application) 
            => application.PatchProviderObjectSpaceCreated<INonsecuredObjectSpaceProvider>( nameof(INonsecuredObjectSpaceProvider.CreateNonsecuredObjectSpace),_ => true,nameof(CreateObjectSpace));

        private static void PatchUpdatingObjectSpaceCreated(this XafApplication application) 
            => application.PatchProviderObjectSpaceCreated<IObjectSpaceProvider>( nameof(IObjectSpaceProvider.CreateUpdatingObjectSpace),info => {
                var parameterInfos = info.Parameters();
                return info.DeclaringType != typeof(NonPersistentObjectSpaceProvider) && info.IsPublic && parameterInfos.Count == 1 &&
                       parameterInfos.Any(parameterInfo => parameterInfo.ParameterType == typeof(bool));
            },nameof(CreateUpdatingObjectSpace));

        private static void PatchProviderObjectSpaceCreated<TDeclaringType>(this XafApplication application, string targetMethodName,Func<MethodInfo,bool> match,string patchMethodName) 
            => application.ObjectSpaceProviders.SelectMany(provider => {
                    var name = $"{typeof(TDeclaringType).FullName}.{targetMethodName}";
                    return provider.GetType().Methods(targetMethodName, name)
                        .Where(match)
                        .OrderBy(info => info.Parameters().Count).Take(1);
                })
                .ForEach(methodInfo => new HarmonyMethod(typeof(ObjectSpaceProviderExtensions), patchMethodName)
                    .PostFix(methodInfo, true));

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static void CreateObjectSpace(IObjectSpaceProvider __instance,IObjectSpace __result) 
            => ObjectSpaceCreatedSubject.OnNext((__result, __instance,false));
        
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static void CreateUpdatingObjectSpace(IObjectSpaceProvider __instance,IObjectSpace __result) 
            => ObjectSpaceCreatedSubject.OnNext((__result, __instance,true));

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
            => SchemaUpdatingSubject.AsObservable().Where(spaceProvider => spaceProvider.IsSame(provider)).Cast<TProvider>();

        public static IObservable<TProvider> WhenSchemaUpdated<TProvider>(this TProvider provider) where TProvider:IObjectSpaceProvider 
            => SchemaUpdatedSubject.AsObservable().Where(spaceProvider => spaceProvider.IsSame(provider)).Cast<TProvider>();
        
        public static IObservable<IObjectSpace> WhenObjectSpaceCreated<TProvider>(this TProvider provider, bool emitUpdatingObjectSpace = false) where TProvider : IObjectSpaceProvider 
            => ObjectSpaceCreatedSubject.Where(t => emitUpdatingObjectSpace || !t.updating)
                .Where(t => t.objectSpaceProvider.IsSame( provider)).Select(t => t.objectSpace);

        static bool IsSame<TExpected, TActual>(this TActual actual, TExpected expected) where TExpected : TActual 
            => EqualityComparer<TExpected>.Default.Equals((TExpected)actual, expected);

        public static IObservable<IDataStore> WhenDataStoreCreated<TProvider>(this TProvider provider) where TProvider:IObjectSpaceProvider 
            => provider.GetPropertyValue("DataStoreProvider").When("DevExpress.ExpressApp.Xpo.ConnectionStringDataStoreProvider")
                .SelectMany(spaceProvider => spaceProvider.WhenEvent("DataStoreCreated").Select(pattern => pattern.EventArgs.GetPropertyValue("DataStore")))
                .Cast<IDataStore>();

    }
}