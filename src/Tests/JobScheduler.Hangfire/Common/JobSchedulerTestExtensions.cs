using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor;
using Fasterflect;
using Hangfire;
using NUnit.Framework;
using Xpand.Extensions.EventArgExtensions;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.NonPersistentObjects;
using Xpand.XAF.Modules.JobScheduler.Hangfire.BusinessObjects;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire.Tests.Common {
    public static class JobSchedulerTestExtensions {
        public static IObservable<Unit> ExecuteAction(this BlazorApplication application,Job job) 
            => Unit.Default.ReturnObservable()
                .Delay(TimeSpan.FromMilliseconds(300))
                .SelectMany(_ => ((IObservable<Unit>)typeof(JobSchedulerService).Method("ExecuteAction", new []{typeof(XafApplication),typeof(string)},Flags.StaticPrivate)
                    .Call(new object[] { application, job.Id })).Select(unit => unit));

        public static IObservable<JobState> Executed(this WorkerState lastState,Func<Job,bool> job=null) 
            => JobSchedulerService.JobState.FirstAsync(jobState => jobState.Fit(job, WorkerState.Enqueued)).IgnoreElements()
                .Concat(JobSchedulerService.JobState.FirstAsync(jobState => jobState.Fit(job, WorkerState.Processing)).IgnoreElements())
                .Concat(JobSchedulerService.JobState.FirstAsync(jobState => jobState.Fit(job, lastState)))
                .FirstAsync();

        private static bool Fit(this JobState jobState, Func<Job, bool> job, WorkerState workerState) 
            => jobState.State == workerState && (job == null || job(jobState.JobWorker.Job));

        public static IObservable<GenericEventArgs<IObservable<Job>>> Handle(this IObservable<GenericEventArgs<IObservable<Job>>> source)
            => source.Do(e => e.Handled = true);

        public static Job CommitNewJob(this BlazorApplication application,Type testJobType=null,string methodName=null,Action<Job> modify=null) {
            testJobType ??= typeof(TestJobDI);
            methodName??=nameof(TestJob.Test);
            var objectSpace = application.CreateObjectSpace();
            var job = objectSpace.CreateObject<Job>();
            job.JobType = new ObjectType(testJobType);
            job.JobMethod = new ObjectString(methodName);
            job.CronExpression = job.ObjectSpace.GetObjectsQuery<CronExpression>()
                .First(expression => expression.Name == nameof(Cron.Yearly));
            job.Id = ScheduledJobId;
            modify?.Invoke(job);
            objectSpace.CommitChanges();
            return job;
        }
        
        public static string ScheduledJobId => $"{TestContext.CurrentContext.Test.MethodName}{TestContext.CurrentContext.Test.ID}";

        public static IObservable<Job> ScheduleImmediate(this
            IObservable<GenericEventArgs<IObservable<Job>>> source, Expression<Action> expression)
            => source.Handle().SelectMany(args => args.Instance.Select(job => {
                job.AddOrUpdateHangfire();
                if (job.CronExpression.Name != nameof(Cron.Never)) {
                    job.Trigger();    
                }
                return job;

            }));

    }
}