using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using JetBrains.Annotations;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.RefreshView {
    [PublicAPI]
    public sealed class RefreshViewModule : ReactiveModuleBase{
        public const string CategoryName = "Xpand.XAF.Modules.RefreshView";
        public static ReactiveTraceSource TraceSource{ get; set; }

        static RefreshViewModule(){
            TraceSource=new ReactiveTraceSource(nameof(RefreshViewModule));
        }
        public RefreshViewModule() {
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.SystemModule.SystemModule));
            RequiredModuleTypes.Add(typeof(ReactiveModule));
            
        }

        public override void Setup(ApplicationModulesManager moduleManager){
            base.Setup(moduleManager);
            Application?.Connect()
                .TakeUntilDisposed(this)
                .Subscribe();
        }

        public override void ExtendModelInterfaces(ModelInterfaceExtenders extenders){
            base.ExtendModelInterfaces(extenders);
            extenders.Add<IModelReactiveModules,IModelReactiveModuleRefreshView>();
        }
    }
}
