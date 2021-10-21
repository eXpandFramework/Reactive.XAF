using System;
using System.Linq.Expressions;
using System.Reactive.Linq;
using DevExpress.ExpressApp.Blazor;
using NUnit.Framework;
using Xpand.Extensions.EventArgExtensions;
using Xpand.Extensions.XAF.NonPersistentObjects;
using Xpand.XAF.Modules.JobScheduler.Hangfire.BusinessObjects;
using Xpand.XAF.Modules.Reactive;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire.Tests.Common {
    public static class JobSchedulerTestExtensions {
        public static IObservable<JobState> Executed(this WorkerState lastState) 
            => JobSchedulerService.JobState.FirstAsync(t => t.State == WorkerState.Enqueued).IgnoreElements()
                .Concat(JobSchedulerService.JobState.FirstAsync(t => t.State == WorkerState.Processing).IgnoreElements())
                .Concat(JobSchedulerService.JobState.FirstAsync(t => t.State == lastState))
                .FirstAsync();

        public static IObservable<GenericEventArgs<IObservable<Job>>> Handle(this IObservable<GenericEventArgs<IObservable<Job>>> source)
            => source.Do(e => e.Handled = true).Select(args => args);

        public static Job CommitNewJob(this BlazorApplication application,Type testJobType=null,string methodName=null) {
            testJobType ??= typeof(TestJobDI);
            methodName??=nameof(TestJob.Test);
            var objectSpace = application.CreateObjectSpace();
            var job = objectSpace.CreateObject<Job>();
            job.JobType = new ObjectType(testJobType);
            job.JobMethod = new ObjectString(methodName);
            job.Id = ScheduledJobId;
            objectSpace.CommitChanges();
            return job;
        }

        public static void ConfigureModel(this BlazorApplication newBlazorApplication) {
            var source = newBlazorApplication.Model.ToReactiveModule<IModelReactiveModulesJobScheduler>().JobScheduler.Sources
                .AddNode<IModelJobSchedulerSource>();
            source.AssemblyName = typeof(JobSchedulerTestExtensions).Assembly.GetName().Name;
        }

        public static string ScheduledJobId => $"{TestContext.CurrentContext.Test.MethodName}{TestContext.CurrentContext.Test.ID}";

        public static IObservable<Job> ScheduleImmediate(this 
            IObservable<GenericEventArgs<IObservable<Job>>> source,Expression<Action> expression)
            => source.Handle().SelectMany(args => args.Instance.Select(job => {
                job.AddOrUpdateHangfire();
                job.Trigger();
                return job;

            })).FirstAsync();

    }
}