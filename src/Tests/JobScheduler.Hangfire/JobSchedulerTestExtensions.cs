using System;
using System.Linq.Expressions;
using System.Reactive.Linq;
using DevExpress.ExpressApp.Blazor;
using NUnit.Framework;
using Xpand.Extensions.EventArgExtensions;
using Xpand.Extensions.XAF.NonPersistentObjects;
using Xpand.XAF.Modules.JobScheduler.Hangfire.BusinessObjects;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire.Tests {
    public static class JobSchedulerTestExtensions {
        private static string _scheduledJobId;

        public static IObservable<GenericEventArgs<IObservable<ScheduledJob>>> Handle(this IObservable<GenericEventArgs<IObservable<ScheduledJob>>> source)
            => source.Do(e => e.Handled = true).Select(args => args);

        public static ScheduledJob CommitNewJob(this BlazorApplication application,Type testJobType=null,string methodName=null) {
            testJobType ??= typeof(TestJobDI);
            methodName??=nameof(TestJob.Test);
            using var objectSpace = application.CreateObjectSpace();
            var scheduledJob = objectSpace.CreateObject<ScheduledJob>();
            scheduledJob.JobType = new ObjectType(testJobType);
            scheduledJob.JobMethod = new ObjectString(methodName);
            _scheduledJobId = ScheduledJobId;
            scheduledJob.Id = _scheduledJobId;
            objectSpace.CommitChanges();
            return scheduledJob;
        }

        public static string ScheduledJobId => $"{TestContext.CurrentContext.Test.MethodName}{TestContext.CurrentContext.Test.ID}";

        public static IObservable<ScheduledJob> ScheduleImmediate(this 
            IObservable<GenericEventArgs<IObservable<ScheduledJob>>> source,Expression<Action> expression)
            => source.Handle().SelectMany(args => args.Instance.Select(job => {
                job.AddOrUpdateHangfire();
                job.Trigger();
                return job;
                // return ScheduledJobService.JobExecution.FirstAsync()
                // .Do(state => job.Trigger()).To(job);

            })).FirstAsync();

    }
}