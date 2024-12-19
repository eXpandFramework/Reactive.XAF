using System;
using System.ComponentModel;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;

using Xpand.Extensions.Reactive.Conditional;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Extensions;
using Xpand.XAF.Modules.Windows.Editors;

namespace Xpand.XAF.Modules.Windows {
    
    public sealed class WindowsModule : ReactiveModuleBase{
        static WindowsModule() => TraceSource=new ReactiveTraceSource(nameof(WindowsModule));
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public static ReactiveTraceSource TraceSource{ get; set; }
        public WindowsModule() {
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.SystemModule.SystemModule));
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.Win.SystemModule.SystemWindowsFormsModule));
            RequiredModuleTypes.Add(typeof(ReactiveModule));
        }

        public override void Setup(ApplicationModulesManager moduleManager){
            base.Setup(moduleManager);
            moduleManager.WindowsConnect()
	            .Merge(moduleManager.ConnectAlertForm())
	            .Merge(moduleManager.EditorsConnect())
                .TakeUntilDisposed(this)
                .Subscribe();
        }

        public override void ExtendModelInterfaces(ModelInterfaceExtenders extenders){
            base.ExtendModelInterfaces(extenders);
            extenders.Add<IModelReactiveModules,IModelReactiveModuleWindows>();
        }
    }
}
