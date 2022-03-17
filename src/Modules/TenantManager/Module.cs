using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.Persistent.Base;
using JetBrains.Annotations;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.Extensions.Reactive.Transform;
using Xpand.XAF.Modules.Blazor;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.TenantManager{
    [UsedImplicitly]
    public sealed class TenantManagerModule : ReactiveModuleBase{
        
        [PublicAPI]
        public static ReactiveTraceSource TraceSource{ get; set; }
        static TenantManagerModule(){
            TraceSource=new ReactiveTraceSource(nameof(TenantManagerModule));
        }
        
        public TenantManagerModule(){
            RequiredModuleTypes.Add(typeof(SystemModule));
            RequiredModuleTypes.Add(typeof(ReactiveModule));
            RequiredModuleTypes.Add(typeof(BlazorModule));
        }

        public override void Setup(ApplicationModulesManager moduleManager){
            base.Setup(moduleManager);
            moduleManager.Connect()
                .TakeUntilDisposed(this)
                .Subscribe();
        }
        
        public override IList<PopupWindowShowAction> GetStartupActions() {
            var actions = new List<PopupWindowShowAction>(base.GetStartupActions());
            if (!((ISecurityUserWithRoles)SecuritySystem.CurrentUser).Roles.Cast<IPermissionPolicyRole>().Any(role => role.IsAdministrative)) {
                Application.LastOrganization()
                    .SwitchIfEmpty(Observable.Defer(() => {
                        var startupAction = new PopupWindowShowAction();
                        actions.Add(startupAction);
                        return startupAction.CreateStartupView().To<object>();
                    }).IgnoreElements())
                    .SelectMany(org => Application.Logon(org))
                    .Subscribe(this);
            }
            return actions;
        }

        public override void ExtendModelInterfaces(ModelInterfaceExtenders extenders){
            base.ExtendModelInterfaces(extenders);
            extenders.Add<IModelReactiveModules,IModelReactiveModulesTenantManager>();
        }
    }
}