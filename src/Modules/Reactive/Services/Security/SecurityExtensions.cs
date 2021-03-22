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
using Xpand.Extensions.EventArgExtensions;
using Xpand.Extensions.ObjectExtensions;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.AppDomainExtensions;
using Xpand.Extensions.XAF.ApplicationModulesManagerExtensions;
using Xpand.Extensions.XAF.SecurityExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.Reactive.Services.Security{
    public static class SecurityExtensions{
        internal static readonly ISubject<(object authentication, GenericEventArgs<object> args)>
            AuthenticateSubject = Subject.Synchronize(new Subject<(object authentication, GenericEventArgs<object> args)>());

        internal static IObservable<Unit> PatchAuthentication(this XafApplication application) 
            => application.WhenSetupComplete()
                .Do(_ => {
                    AppDomain.CurrentDomain.Patch(harmony => {
                        if (application.Security.IsInstanceOf("DevExpress.ExpressApp.Security.SecurityStrategyBase")){
                            var methodInfo = application.Security?.GetPropertyValue("Authentication")?.GetType().Methods("Authenticate")
                                .Last(info =>info.DeclaringType!=null&& !info.DeclaringType.IsAbstract);
                            if (methodInfo != null){
                                harmony.Patch(methodInfo, new HarmonyMethod(GetMethodInfo(nameof(Authenticate))));	
                            }	
                        }
                    });
                })
                .ToUnit();

        static MethodInfo GetMethodInfo(string methodName) 
            => typeof(SecurityExtensions).GetMethods(BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public).First(info => info.Name == methodName);

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static bool Authenticate(ref object __result,object __instance,IObjectSpace objectSpace) {
            var args = new GenericEventArgs<object>();
            AuthenticateSubject.OnNext((__instance, args));
            if (args.Instance != null){
                __result = objectSpace.GetObjectByKey((Type) __instance.GetPropertyValue("UserType"), args.Instance);
                return false;
            }
            return true;
        }

        internal static IObservable<Unit> AddNonSecuredTypes(this ApplicationModulesManager applicationModulesManager) 
            => applicationModulesManager.WhenCustomizeTypesInfo()
                .Select(_ =>_.e.TypesInfo.PersistentTypes.Where(info => info.Attributes.OfType<NonSecuredTypeAttribute>().Any())
                    .Select(info => info.Type))
                .Do(infos => {
                    var xafApplication = applicationModulesManager.Application();
                    xafApplication?.AddNonSecuredType(infos.ToArray());
                })
                .ToUnit();

        public static IObservable<Unit> Logon(this XafApplication application,object userKey) =>
            AuthenticateSubject.Where(_ => _.authentication== application.Security.GetPropertyValue("Authentication"))
                .Do(_ => _.args.Instance=userKey).SelectMany(_ => application.WhenLoggedOn().FirstAsync()).ToUnit()
                .Merge(Unit.Default.ReturnObservable().Do(_ => application.Logon()).IgnoreElements())
                .TraceRX(_ => $"{userKey}");
    }
}