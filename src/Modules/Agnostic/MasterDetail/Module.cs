using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.SystemModule;
using Xpand.XAF.Modules.Reactive;

namespace Xpand.XAF.Modules.MasterDetail{
    public sealed class MasterDetailModule : ModuleBase{
        public const string CategoryName = "Xpand.XAF.Modules.MasterDetail";

        public MasterDetailModule(){

            RequiredModuleTypes.Add(typeof(SystemModule));
            RequiredModuleTypes.Add(typeof(ReactiveModule));
            
        }

        public override void Setup(ApplicationModulesManager moduleManager){
            base.Setup(moduleManager);
            moduleManager.Connect().Subscribe();
        }


        public override void ExtendModelInterfaces(ModelInterfaceExtenders extenders){
            base.ExtendModelInterfaces(extenders);
            extenders.Add<IModelDashboardView, IModelDashboardViewMasterDetail>();
            extenders.Add<IModelApplication, IModelApplicationMasterDetail>();
        }
    }
}