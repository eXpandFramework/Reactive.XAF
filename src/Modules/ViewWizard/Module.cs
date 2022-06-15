using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;

using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.ViewWizard {
    
    public sealed class ViewWizardModule : ReactiveModuleBase {
        static ViewWizardModule(){
            TraceSource=new ReactiveTraceSource(nameof(ViewWizardModule));
        }
        
        public static ReactiveTraceSource TraceSource{ get; set; }
        public ViewWizardModule() {
            RequiredModuleTypes.Add(typeof(ReactiveModule));
        }

        public override void Setup(ApplicationModulesManager moduleManager){
            base.Setup(moduleManager);
            moduleManager.Connect()
                .Subscribe(this);
        }


        public override void ExtendModelInterfaces(ModelInterfaceExtenders extenders){
            base.ExtendModelInterfaces(extenders);
            extenders.Add<IModelReactiveModules,IModelReactiveModulesViewWizard>();
        }

    }
    
}
