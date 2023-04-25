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
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.EventArgExtensions;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.NonPersistentObjects;
using Xpand.XAF.Modules.JobScheduler.Hangfire.BusinessObjects;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire.Tests.Common {
    public static class JobSchedulerTestExtensions {
        public static IObservable<Unit> ExecuteAction(this BlazorApplication application,Job job) 
            => Unit.Default.ReturnObservable().Delay(TimeSpan.FromMilliseconds(300))
                .SelectMany(_ => ((IObservable<Unit>)AppDomain.CurrentDomain.GetAssemblyType("Xpand.XAF.Modules.JobScheduler.Hangfire.ExecuteJobActionExtensions")
                    .Method("ExecuteAction", new []{typeof(XafApplication),typeof(string)},Flags.StaticPrivate)
                    .Call(new object[] { application, job.Id })).Select(unit => unit));

        public static IObservable<JobState> Executed(this WorkerState lastState,Func<Job,bool> job=null) 
            => JobSchedulerService.JobState.FirstAsync(t => t.Fit(null, WorkerState.Enqueued)).IgnoreElements()
                .Concat(Observable.Defer(() => JobSchedulerService.JobState.FirstAsync(jobState => jobState.Fit(null, WorkerState.Processing)).IgnoreElements()))
                .Concat(Observable.Defer(() => JobSchedulerService.JobState.FirstAsync(jobState => jobState.Fit(job, lastState))))
                .FirstAsync();

        private static bool Fit(this JobState jobState, Func<Job, bool> job, WorkerState workerState) 
            => jobState.State == workerState && (job == null || job(jobState.JobWorker.Job));

        public static IObservable<GenericEventArgs<IObservable<Job>>> Handle(this IObservable<GenericEventArgs<IObservable<Job>>> source)
            => source.Do(e => e.Handled = true);

        public static Job CommitNewJob(this BlazorApplication application,Type testJobType=null,string methodName=null,Action<Job> modify=null) {
            testJobType ??= typeof(TestJobDI);
            methodName??=nameof(TestJob.TestJobId);
            var objectSpace = application.CreateObjectSpace();
            var job = objectSpace.CreateObject<Job>();
            job.JobType = new ObjectType(testJobType);
            job.JobMethod = new ObjectString(methodName);
            job.CronExpression = job.ObjectSpace.GetObjectsQuery<CronExpression>()
                .FirstOrDefault(expression => expression.Name == nameof(Cron.Minutely));
            job.Id = ScheduledJobId;
            modify?.Invoke(job);
            objectSpace.CommitChanges();
            return job;
        }
        
        public static string ScheduledJobId => $"{TestContext.CurrentContext.Test.MethodName}{TestContext.CurrentContext.Test.ID}";

        public static IObservable<Job> ScheduleImmediate(this
            IObservable<GenericEventArgs<IObservable<Job>>> source, Expression<Action> expression,IServiceProvider serviceProvider)
            => source.Handle().SelectMany(args => args.Instance.SelectMany(job => {
                job.AddOrUpdateHangfire(serviceProvider);
                if (job.CronExpression.Name != nameof(Cron.Never)) {
                    return Observable.Start(job.Trigger).To(job);
                }
                return job.ReturnObservable();

            }));

    }
}