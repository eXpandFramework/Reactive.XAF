using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor;
using Hangfire;
using Hangfire.MemoryStorage;
using Xpand.TestsLib.Blazor;
using Xpand.XAF.Modules.Reactive;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire.Notification.Tests.Common {
    public abstract class CommonTest : BlazorCommonTest {

        protected override void ResetXAF() {
            
        }

        public override void Init() {
            base.Init();
            ReactiveModuleBase.Scheduler=TestScheduler;
            GlobalConfiguration.Configuration.UseMemoryStorage();
        }

        public JobSchedulerNotificationModule JobSchedulerNotificationModule(params ModuleBase[] modules) 
            => NewBlazorApplication().JobSchedulerNotificationModule();

        protected virtual BlazorApplication NewBlazorApplication() 
            => NewBlazorApplication(typeof(Startup));

        

    }
}