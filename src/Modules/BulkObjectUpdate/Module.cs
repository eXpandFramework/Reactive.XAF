using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.SystemModule;
using JetBrains.Annotations;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.BulkObjectUpdate{
    [UsedImplicitly]
    public sealed class BulkObjectUpdateModule : ReactiveModuleBase{
        public static ReactiveTraceSource TraceSource{ get; set; }
        static BulkObjectUpdateModule() 
            => TraceSource=new ReactiveTraceSource(nameof(BulkObjectUpdateModule));

        public BulkObjectUpdateModule(){
            RequiredModuleTypes.Add(typeof(SystemModule));
            RequiredModuleTypes.Add(typeof(ReactiveModule));
        }

        public override void Setup(ApplicationModulesManager moduleManager){
            base.Setup(moduleManager);
            moduleManager.Connect()
                .TakeUntilDisposed(this)
                .Subscribe();
        }
        
        public override void ExtendModelInterfaces(ModelInterfaceExtenders extenders){
            base.ExtendModelInterfaces(extenders);
            extenders.Add<IModelReactiveModules,IModelReactiveModulesBulkObjectUpdate>();
        }
    }
}