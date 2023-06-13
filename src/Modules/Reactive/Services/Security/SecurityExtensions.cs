using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Security.Claims;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Core;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.Security.Authentication.Internal;
using Fasterflect;
using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using Xpand.Extensions.EventArgExtensions;
using Xpand.Extensions.ObjectExtensions;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.ApplicationModulesManagerExtensions;
using Xpand.Extensions.XAF.Harmony;
using Xpand.Extensions.XAF.SecurityExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.Reactive.Services.Security{
    public static class SecurityExtensions{
        internal static readonly ISubject<(object authentication, GenericEventArgs<object> args)>
            AuthenticateSubject = Subject.Synchronize(new Subject<(object authentication, GenericEventArgs<object> args)>());

        internal static IObservable<Unit> PatchAuthentication(this XafApplication application) 
            => application.WhenSetupComplete()
                .DoWhen(_ => application.Security.IsInstanceOf("DevExpress.ExpressApp.Security.SecurityStrategyBase"),
                    _ => new HarmonyMethod(typeof(SecurityExtensions),nameof(AuthenticatePatch))
                        .PreFix(application.Security.GetPropertyValue("Authentication").GetType().Methods("Authenticate")
                            .Last(info => info.DeclaringType is { IsAbstract: false }),true)
                        )
                .ToUnit();
        
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static bool AuthenticatePatch(ref object __result,object __instance,IObjectSpace objectSpace) {
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
                .Select(e =>e.TypesInfo.PersistentTypes.Where(info => info.Attributes.OfType<NonSecuredTypeAttribute>().Any())
                    .Select(info => info.Type))
                .Do(infos => {
                    var xafApplication = applicationModulesManager.Application();
                    xafApplication?.AddNonSecuredType(infos.ToArray());
                })
                .ToUnit();

        public static IObservable<Unit> Logon(this XafApplication application,object userKey) 
            => AuthenticateSubject.Where(_ => _.authentication== application.Security.GetPropertyValue("Authentication"))
                .Do(_ => _.args.SetInstance(_ => userKey)).SelectMany(_ => application.WhenLoggedOn().FirstAsync()).ToUnit()
                .Merge(Unit.Default.Observe().Do(_ => application.Logon()).IgnoreElements())
                .TraceRX(_ => $"{userKey}");

        public static IObjectSpace CreateAuthenticatedObjectSpace<TUser>(this IServiceProvider provider, string userName) 
            => provider.CreateAuthenticatedObjectSpace(typeof(TUser), userName);

        public static IObjectSpace CreateAuthenticatedObjectSpace(this IServiceProvider provider,Type userType, string userName) {
            var nonSecuredOS = provider.GetRequiredService<INonSecuredObjectSpaceFactory>().CreateNonSecuredObjectSpace(userType);
            var serviceUser = (ISecurityUser)nonSecuredOS.FindObject(userType,CriteriaOperator.FromLambda<ISecurityUser>(user => user.UserName==userName));
            var xafIdentityCreator = provider.GetRequiredService<IStandardAuthenticationIdentityCreator>();
            var claimsPrincipal = new ClaimsPrincipal(xafIdentityCreator.CreateIdentity(nonSecuredOS.GetKeyValueAsString(serviceUser), serviceUser.UserName));
            ((IPrincipalProviderInitializer)provider.GetRequiredService<IPrincipalProvider>()).SetUser(claimsPrincipal);
            return provider.GetRequiredService<ISecurityProvider>().GetSecurity().LogonObjectSpace;
        }
    }
}