using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.Reactive.Logger {
    public sealed class ReactiveLoggerModule : ReactiveModuleBase{
        public const string CategoryName = "Xpand.XAF.Modules.Reactive.Logger";

        static ReactiveLoggerModule(){
            TraceSource=new ReactiveTraceSource(nameof(ReactiveLoggerModule));
        }
        public ReactiveLoggerModule() {
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.SystemModule.SystemModule));
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.ConditionalAppearance.ConditionalAppearanceModule));
            RequiredModuleTypes.Add(typeof(ReactiveModule));
        }

        public static ReactiveTraceSource TraceSource{ get; set; }
        public override void Setup(ApplicationModulesManager moduleManager){
            base.Setup(moduleManager);
        }

        public override void AddGeneratorUpdaters(ModelNodesGeneratorUpdaters updaters){
            base.AddGeneratorUpdaters(updaters);
            updaters.Add(new TraceEventAppearenceRulesGenerator());
        }

        public override void Setup(XafApplication application){
            base.Setup(application);
            
            this.Connect()
                .TakeUntilDisposed(this)
                .Subscribe();
        }

        public override void ExtendModelInterfaces(ModelInterfaceExtenders extenders){
            base.ExtendModelInterfaces(extenders);
            
            extenders.Add<IModelReactiveModules,IModelReactiveModuleLogger>();
            
        }
    }

}
