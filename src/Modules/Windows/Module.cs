using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using JetBrains.Annotations;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.Windows {
    [UsedImplicitly]
    public sealed class WindowsModule : ReactiveModuleBase{
        static WindowsModule(){
            TraceSource=new ReactiveTraceSource(nameof(WindowsModule));
        }
        public static ReactiveTraceSource TraceSource{ get; [PublicAPI]set; }
        public WindowsModule() {
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.SystemModule.SystemModule));
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.Win.SystemModule.SystemWindowsFormsModule));
            RequiredModuleTypes.Add(typeof(ReactiveModule));
        }

        public override void Setup(ApplicationModulesManager moduleManager){
            base.Setup(moduleManager);
            moduleManager.Connect()
                .TakeUntilDisposed(this)
                .Subscribe();
        }

        public override void ExtendModelInterfaces(ModelInterfaceExtenders extenders){
            base.ExtendModelInterfaces(extenders);
            extenders.Add<IModelReactiveModules,IModelReactiveModuleWindows>();
        }
    }
}
