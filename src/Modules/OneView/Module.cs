using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using JetBrains.Annotations;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.OneView {
    [UsedImplicitly]
    public sealed class OneViewModule : ReactiveModuleBase{
        [PublicAPI]
        public const string CategoryName = "Xpand.XAF.Modules.OneView";

        static OneViewModule(){
            TraceSource=new ReactiveTraceSource(nameof(OneViewModule));
        }
        public static ReactiveTraceSource TraceSource{ get; [PublicAPI]set; }
        public OneViewModule() {
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.SystemModule.SystemModule));
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
