using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor;
using Hangfire;
using Hangfire.MemoryStorage;
using Xpand.Extensions.TypeExtensions;
using Xpand.TestsLib.Blazor;
using Xpand.TestsLib.Common;
using Xpand.XAF.Modules.JobScheduler.Hangfire.BusinessObjects;
using Xpand.XAF.Modules.JobScheduler.Hangfire.Tests.BO;



namespace Xpand.XAF.Modules.JobScheduler.Hangfire.Tests {
    public abstract class JobSchedulerCommonTest : BlazorCommonTest {
        protected override void ResetXAF() {
            
        }

        public JobSchedulerModule JobSchedulerModule(params ModuleBase[] modules) {
            var newBlazorApplication = NewBlazorApplication();
            return JobSchedulerModule(newBlazorApplication);
        }

        protected BlazorApplication NewBlazorApplication() 
            => NewBlazorApplication(typeof(JobSchedulerStartup));

        protected JobSchedulerModule JobSchedulerModule(BlazorApplication newBlazorApplication) {
            var module = newBlazorApplication.AddModule<JobSchedulerModule>(typeof(JS));
            newBlazorApplication.ConfigureModel();
            newBlazorApplication.Logon();
            using var objectSpace = newBlazorApplication.CreateObjectSpace();
            return module;
        }

        protected IObservable<Job> MockHangfire(Type testJobType = null, string testName = null) {
            GlobalConfiguration.Configuration.UseMemoryStorage();
            testJobType ??= typeof(TestJobDI);
            testName ??= nameof(TestJob.Test);

            return JobSchedulerService.CustomJobSchedule.ScheduleImmediate(testJobType.CallExpression(testName));
        }
    }
}