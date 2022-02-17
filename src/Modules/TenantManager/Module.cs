using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.Persistent.Base;
using JetBrains.Annotations;
using Xpand.Extensions.Reactive.Conditional;
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
            if (!((ISecurityUserWithRoles)SecuritySystem.CurrentUser).Roles.Cast<IPermissionPolicyRole>().Any(role => role.IsAdministrative)){
                var startupAction = new PopupWindowShowAction();
                startupAction.CustomizePopupWindowParams += (_, e) => {
                    var model = Application.Model.TenantManager();
                    var startupType = model.StartupView.ModelClass.TypeInfo.Type;
                    var objectSpace = Application.CreateObjectSpace(startupType);
                    e.View = Application.CreateDetailView(objectSpace, model.StartupView,true,objectSpace.CreateObject(startupType));
		        
                };
                actions.Add(startupAction);            
            }
            return actions;
        }

        public override void ExtendModelInterfaces(ModelInterfaceExtenders extenders){
            base.ExtendModelInterfaces(extenders);
            extenders.Add<IModelReactiveModules,IModelReactiveModulesTenantManager>();
        }
    }
}