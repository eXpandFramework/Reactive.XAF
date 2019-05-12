using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.ViewEditMode {
    public sealed class ViewEditModeModule : ModuleBase{
        public const string CategoryName = "Xpand.XAF.Modules.ViewEditMode";

        public ViewEditModeModule() {
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.SystemModule.SystemModule));
            RequiredModuleTypes.Add(typeof(Reactive.ReactiveModule));
        }

        public override void Setup(ApplicationModulesManager moduleManager){
            base.Setup(moduleManager);
            ViewEditModeService.Connect()
                .TakeUntilDisposingMainWindow()
                .Subscribe();
        }

        public override void ExtendModelInterfaces(ModelInterfaceExtenders extenders){
            base.ExtendModelInterfaces(extenders);
            extenders.Add<IModelDetailView,IModelDetailViewViewEditMode>();
        }
    }
}
