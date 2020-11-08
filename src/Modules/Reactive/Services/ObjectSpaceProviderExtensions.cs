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

namespace Xpand.XAF.Modules.Reactive.Services{
    public static class ObjectSpaceProviderExtensions{
        public static IObservable<TResult> NewObjectSpace<TResult>(this IObjectSpaceProvider provider,Func<IObjectSpace, IObservable<TResult>> factory) 
            => provider == null ? Observable.Empty<TResult>() : Observable.Using(provider.CreateObjectSpace, factory);

        private static readonly Subject<IObjectSpaceProvider> SchemaUpdatingSubject=new Subject<IObjectSpaceProvider>();
        private static readonly Subject<IObjectSpaceProvider> SchemaUpdatedSubject=new Subject<IObjectSpaceProvider>();

        private static MethodInfo GetMethodInfo(string methodName) 
            => typeof(ObjectSpaceProviderExtensions).GetMethods(BindingFlags.Static|BindingFlags.NonPublic).First(info => info.Name == methodName);

        internal static IObservable<Unit> PatchObjectSpaceProvider(this XafApplication application) 
            => application.WhenSetupComplete()
                .Do(_ => {
                    AppDomain.CurrentDomain.Patch(harmony => {
                        foreach (var provider in application.ObjectSpaceProviders.Where(provider => !(provider is NonPersistentObjectSpaceProvider))){
                            var methodInfos = provider.GetType().Methods($"{typeof(IObjectSpaceProvider).FullName}.{nameof(IObjectSpaceProvider.UpdateSchema)}")
                                .Concat(provider.GetType().Methods(nameof(IObjectSpaceProvider.UpdateSchema)));
                            var methodInfo = methodInfos
                                .Last(info =>info.DeclaringType!=null&& !info.DeclaringType.IsAbstract);
                            if (methodInfo != null){
                                harmony.Patch(methodInfo, new HarmonyMethod(GetMethodInfo(nameof(SchemaUpdating))));	
                                harmony.Patch(methodInfo, postfix:new HarmonyMethod(GetMethodInfo(nameof(SchemaUpdated))));	
                            }	
                        }
                    });
                })
                .ToUnit();

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

    }
}