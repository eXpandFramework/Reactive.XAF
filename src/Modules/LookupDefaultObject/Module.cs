using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Validation;
using JetBrains.Annotations;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.LookupDefaultObject {
    public sealed class LookupDefaultObjectModule : ReactiveModuleBase {
        static LookupDefaultObjectModule(){
            TraceSource=new ReactiveTraceSource(nameof(LookupDefaultObjectModule));
        }
        [PublicAPI]
        public static ReactiveTraceSource TraceSource{ get; set; }
        public LookupDefaultObjectModule() {
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
            extenders.Add<IModelReactiveModules,IModelReactiveModulesLookupDefaultObject>();
        }

    }
    
}
