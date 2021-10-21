using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor;
using Hangfire;
using Hangfire.MemoryStorage;
using Xpand.Extensions.XAF.TypesInfoExtensions;
using Xpand.TestsLib.Blazor;
using Xpand.TestsLib.Common;
using Xpand.XAF.Modules.JobScheduler.Hangfire.Notification.Tests.BO;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire.Notification.Tests.Common {
    public abstract class CommonTest : BlazorCommonTest {

        protected override void ResetXAF() {
            
        }

        public override void Init() {
            base.Init();
            Notification.JobSchedulerNotificationModule.Scheduler=TestScheduler;
            GlobalConfiguration.Configuration.UseMemoryStorage();
        }

        public JobSchedulerNotificationModule JobSchedulerNotificationModule(params ModuleBase[] modules) {
            var newBlazorApplication = NewBlazorApplication();
            return JobSchedulerNotificationModule(newBlazorApplication);
        }

        protected virtual BlazorApplication NewBlazorApplication() 
            => NewBlazorApplication(typeof(Startup));

        protected JobSchedulerNotificationModule JobSchedulerNotificationModule(BlazorApplication newBlazorApplication) {
            var module = newBlazorApplication.AddModule<JobSchedulerNotificationModule>(typeof(JSNE).CollectExportedTypesFromAssembly().ToArray());
            // newBlazorApplication.ConfigureModel();
            newBlazorApplication.Logon();
            using var objectSpace = newBlazorApplication.CreateObjectSpace();
            return module;
        }

    }
}