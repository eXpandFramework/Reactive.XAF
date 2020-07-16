using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using JetBrains.Annotations;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.ViewWizard {
    [UsedImplicitly]
    public sealed class ViewWizardModule : ReactiveModuleBase {
        static ViewWizardModule(){
            TraceSource=new ReactiveTraceSource(nameof(ViewWizardModule));
        }
        [PublicAPI]
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
