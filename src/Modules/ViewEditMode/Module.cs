using System;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using Xpand.Source.Extensions.XAF;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.ViewEditMode {
    public sealed class ViewEditModeModule : XafModule{
        public const string CategoryName = "Xpand.XAF.Modules.ViewEditMode";

        public ViewEditModeModule() {
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.SystemModule.SystemModule));
            RequiredModuleTypes.Add(typeof(Reactive.ReactiveModule));
        }

        public override void Setup(ApplicationModulesManager moduleManager){
            base.Setup(moduleManager);
            Application.Connect()
                .TakeUntil(this.WhenDisposed())
                .Subscribe();
        }

        public override void ExtendModelInterfaces(ModelInterfaceExtenders extenders){
            base.ExtendModelInterfaces(extenders);
            extenders.Add<IModelDetailView,IModelDetailViewViewEditMode>();
        }
    }
}
