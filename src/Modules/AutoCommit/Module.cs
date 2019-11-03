using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.SystemModule;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.AutoCommit{
    public sealed class AutoCommitModule : ReactiveModuleBase{
        public const string CategoryName = "Xpand.XAF.Modules.AutoCommit";

        public AutoCommitModule(){
            RequiredModuleTypes.Add(typeof(SystemModule));
            RequiredModuleTypes.Add(typeof(ReactiveModule));   
        }
        public static ReactiveTraceSource TraceSource{ get; set; }
        static AutoCommitModule(){
            TraceSource=new ReactiveTraceSource(nameof(AutoCommitModule));
        }

        public override void Setup(ApplicationModulesManager moduleManager){
            base.Setup(moduleManager);
            Application.Connect()
                .TakeUntilDisposed(this)
                .Subscribe(unit => { }, () => { });
        }

        public override void ExtendModelInterfaces(ModelInterfaceExtenders extenders){
            base.ExtendModelInterfaces(extenders);
            extenders.Add<IModelClass, IModelClassAutoCommit>();
            extenders.Add<IModelObjectView, IModelObjectViewAutoCommit>();
        }

    }
}