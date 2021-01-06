using System;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.AspNetCore.Hosting;
using Xpand.Extensions.Blazor;
using Xpand.Extensions.TypeExtensions;
using Xpand.TestsLib.Blazor;
using Xpand.TestsLib.Common;
using Xpand.XAF.Modules.JobScheduler.Hangfire.BusinessObjects;
using Xpand.XAF.Modules.JobScheduler.Hangfire.Tests.BO;

[assembly: HostingStartup(typeof(Xpand.XAF.Modules.JobScheduler.Hangfire.HangfireStartup))]
[assembly: HostingStartup(typeof(HostingStartup))]

namespace Xpand.XAF.Modules.JobScheduler.Hangfire.Tests {
    public abstract class JobSchedulerCommonTest : BlazorCommonTest {
        protected override void ResetXAF() {
            
        }

        protected  IObservable<JobState> JobExecution(WorkerState lastState) 
            => JobService.JobExecution.FirstAsync(t => t.State == WorkerState.Enqueued).IgnoreElements()
                .Concat(JobService.JobExecution.FirstAsync(t => t.State == WorkerState.Processing).IgnoreElements())
                .Concat(JobService.JobExecution.FirstAsync(t => t.State == lastState))
                .FirstAsync();

        public JobSchedulerModule JobSchedulerModule(params ModuleBase[] modules) {
            var newBlazorApplication = NewBlazorApplication(typeof(JobSchedulerStartup));
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

            return JobService.CustomJobSchedule.ScheduleImmediate(testJobType.CallExpression(testName));
        }
    }
}