using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;

using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.PositionInListView {
    
    public sealed class PositionInListViewModule : ReactiveModuleBase {
        static PositionInListViewModule(){
            TraceSource=new ReactiveTraceSource(nameof(PositionInListViewModule));
        }
        
        public static ReactiveTraceSource TraceSource{ get; set; }
        public PositionInListViewModule() {
            RequiredModuleTypes.Add(typeof(ReactiveModule));
        }

        public override void Setup(ApplicationModulesManager moduleManager){
            base.Setup(moduleManager);
            moduleManager.Connect()
                .Merge(moduleManager.SwapPosition())
                .Subscribe(this);
        }
        
        public override void ExtendModelInterfaces(ModelInterfaceExtenders extenders){
            base.ExtendModelInterfaces(extenders);
            extenders.Add<IModelReactiveModules,IModelReactiveModulesPositionInListView>();
        }

    }
    
}
