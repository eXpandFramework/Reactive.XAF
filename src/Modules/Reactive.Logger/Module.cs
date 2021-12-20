using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using JetBrains.Annotations;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.Reactive.Logger {
    [UsedImplicitly]
    public sealed class ReactiveLoggerModule : ReactiveModuleBase{
        [PublicAPI]
        public const string CategoryName = "Xpand.XAF.Modules.Reactive.Logger";

        static ReactiveLoggerModule(){
            TraceSource=new ReactiveTraceSource(nameof(ReactiveLoggerModule));
        }
        public ReactiveLoggerModule() {
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.SystemModule.SystemModule));
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.ConditionalAppearance.ConditionalAppearanceModule));
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.Notifications.NotificationsModule));
            RequiredModuleTypes.Add(typeof(ReactiveModule));
            
        }

        [PublicAPI]
        public static ReactiveTraceSource TraceSource{ get; set; }

        public override void AddGeneratorUpdaters(ModelNodesGeneratorUpdaters updaters){
            base.AddGeneratorUpdaters(updaters);
            updaters.Add(new TraceEventAppearanceRulesGenerator());
        }

        public override void Setup(ApplicationModulesManager manager){
            base.Setup(manager);
            manager.Connect().Subscribe(this);
        }
 
        public override void ExtendModelInterfaces(ModelInterfaceExtenders extenders){
            base.ExtendModelInterfaces(extenders);
            
            extenders.Add<IModelReactiveModules,IModelReactiveModuleLogger>();
            
        }
    }

}
