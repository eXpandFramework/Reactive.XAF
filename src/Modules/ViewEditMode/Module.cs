using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using JetBrains.Annotations;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.ViewEditMode {
    [UsedImplicitly]
    public sealed class ViewEditModeModule : ReactiveModuleBase{
        public const string CategoryName = "Xpand.XAF.Modules.ViewEditMode";

        public ViewEditModeModule() {
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.SystemModule.SystemModule));
            RequiredModuleTypes.Add(typeof(ReactiveModule));
        }
        public static ReactiveTraceSource TraceSource{ get; [PublicAPI]set; }
        static ViewEditModeModule(){
            TraceSource=new ReactiveTraceSource(nameof(ViewEditModeModule));
        }
        public override void Setup(ApplicationModulesManager moduleManager){
            base.Setup(moduleManager);
            moduleManager.Connect()
                .TakeUntilDisposed(this)
                .Subscribe();
        }

        public override void ExtendModelInterfaces(ModelInterfaceExtenders extenders){
            base.ExtendModelInterfaces(extenders);
            extenders.Add<IModelDetailView,IModelDetailViewViewEditMode>();
        }
    }
}
