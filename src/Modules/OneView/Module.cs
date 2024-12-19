using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Extensions;
using System.ComponentModel;

namespace Xpand.XAF.Modules.OneView {
    
    public sealed class OneViewModule : ReactiveModuleBase{
        
        public const string CategoryName = "Xpand.XAF.Modules.OneView";

        static OneViewModule(){
            TraceSource=new ReactiveTraceSource(nameof(OneViewModule));
        }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public static ReactiveTraceSource TraceSource{ get; set; }
        public OneViewModule() {
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
            extenders.Add<IModelReactiveModules,IModelReactiveModuleOneView>();
        }
    }
}
