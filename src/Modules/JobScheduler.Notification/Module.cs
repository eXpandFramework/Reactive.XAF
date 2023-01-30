using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;

using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire.Notification {
    public sealed class JobSchedulerNotificationModule : ReactiveModuleBase {

        static JobSchedulerNotificationModule() => TraceSource=new ReactiveTraceSource(nameof(JobSchedulerNotificationModule));

        public static ReactiveTraceSource TraceSource{ get; set; }
        public JobSchedulerNotificationModule() {
            RequiredModuleTypes.Add(typeof(JobSchedulerModule));
        }
        
        public override void Setup(ApplicationModulesManager moduleManager){
            base.Setup(moduleManager);
            moduleManager.Connect()
                .Subscribe(this);
        }
        
        public override void ExtendModelInterfaces(ModelInterfaceExtenders extenders){
            base.ExtendModelInterfaces(extenders);
            extenders.Add<IModelJobScheduler,IModelJobSchedulerNotification>();
        }

    }
}
