using System;
using DevExpress.ExpressApp;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Xpand.Extensions.Blazor;
using Xpand.Extensions.TypeExtensions;
using Xpand.TestsLib.Blazor;
using Xpand.TestsLib.Common;
using Xpand.XAF.Modules.JobScheduler.Hangfire.BusinessObjects;
using Xpand.XAF.Modules.JobScheduler.Hangfire.Tests.BO;

[assembly: HostingStartup(typeof(Xpand.XAF.Modules.JobScheduler.Hangfire.JobSchedulerStartup))]
[assembly: HostingStartup(typeof(HostingStartup))]

namespace Xpand.XAF.Modules.JobScheduler.Hangfire.Tests {
    public class JobSchedulerStartup : XafHostingStartup<JobSchedulerModule> {
        public JobSchedulerStartup(IConfiguration configuration) : base(configuration) { }
    }

    public abstract class JobSchedulerCommonTest : BlazorCommonTest {
        protected BackgroundJobServer BackgroundJobServer;

        protected JobSchedulerModule JobSchedulerModule(params ModuleBase[] modules) {
            var newBlazorApplication = NewBlazorApplication(typeof(JobSchedulerStartup));
            var module = newBlazorApplication.AddModule<JobSchedulerModule>(typeof(JS));
            newBlazorApplication.Logon();
            using var objectSpace = newBlazorApplication.CreateObjectSpace();
            return module;
        }

        protected IObservable<Job> MockHangfire(Type testJobType = null, string testName = null) {
            GlobalConfiguration.Configuration.UseMemoryStorage();
            testJobType ??= typeof(TestJobDI);
            testName ??= nameof(TestJob.Test);

            return JobService.CustomJobSchedule.ScheduleImmediate(testJobType.CallExpression(testName));
        }
    }
}