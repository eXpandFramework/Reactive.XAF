using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Validation;

using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.ViewItemValue {
    
    public sealed class ViewItemValueModule : ReactiveModuleBase {
        static ViewItemValueModule(){
            TraceSource=new ReactiveTraceSource(nameof(ViewItemValueModule));
        }
        
        public static ReactiveTraceSource TraceSource{ get; set; }
        public ViewItemValueModule() {
            RequiredModuleTypes.Add(typeof(ReactiveModule));
            RequiredModuleTypes.Add(typeof(ValidationModule));
        }

        public override void Setup(ApplicationModulesManager moduleManager){
            base.Setup(moduleManager);
            moduleManager.Connect()
                .Subscribe(this);
        }

        public override void ExtendModelInterfaces(ModelInterfaceExtenders extenders){
            base.ExtendModelInterfaces(extenders);
            extenders.Add<IModelReactiveModules,IModelReactiveModulesViewItemValue>();
        }

    }
    
}
