using System;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.SystemModule;
using Xpand.Source.Extensions.XAF;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.MasterDetail{
    public sealed class MasterDetailModule : XafModule{
        public const string CategoryName = "Xpand.XAF.Modules.MasterDetail";

        public MasterDetailModule(){

            RequiredModuleTypes.Add(typeof(SystemModule));
            RequiredModuleTypes.Add(typeof(ReactiveModule));
            
        }

        public override void Setup(ApplicationModulesManager moduleManager){
            base.Setup(moduleManager);
            moduleManager.Connect(Application)
                .TakeUntil(this.WhenDisposed())
                .Subscribe();
        }


        public override void ExtendModelInterfaces(ModelInterfaceExtenders extenders){
            base.ExtendModelInterfaces(extenders);
            extenders.Add<IModelDashboardView, IModelDashboardViewMasterDetail>();
            extenders.Add<IModelApplication, IModelApplicationMasterDetail>();
        }
    }
}