using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using JetBrains.Annotations;
using Xpand.XAF.Modules.Email;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire.Notification.Email {
    public sealed class EmailNotificationModule : ReactiveModuleBase {

        static EmailNotificationModule() => TraceSource=new ReactiveTraceSource(nameof(EmailNotificationModule));
        

        [PublicAPI]
        public static ReactiveTraceSource TraceSource{ get; set; }
        public EmailNotificationModule() {
            RequiredModuleTypes.Add(typeof(JobSchedulerNotificationModule));
            RequiredModuleTypes.Add(typeof(EmailModule));
        }
        
        public override void Setup(ApplicationModulesManager moduleManager){
            base.Setup(moduleManager);
            moduleManager.Connect()
                .Subscribe(this);
        }
        
        public override void ExtendModelInterfaces(ModelInterfaceExtenders extenders){
            base.ExtendModelInterfaces(extenders);
            extenders.Add<IModelNotification,IModelNotificationEmail>();
        }

    }
}
