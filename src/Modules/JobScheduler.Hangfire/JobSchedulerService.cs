using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Runtime.CompilerServices;
using DevExpress.DataAccess.Native.ObjectBinding;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Blazor;
using DevExpress.ExpressApp.Utils;
using Fasterflect;
using Hangfire;
using Hangfire.Server;
using Hangfire.States;
using Hangfire.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Xpand.Extensions.Blazor;
using Xpand.Extensions.EventArgExtensions;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.TypeExtensions;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.ObjectSpaceExtensions;
using Xpand.Extensions.XAF.Xpo.BaseObjects;
using Xpand.XAF.Modules.JobScheduler.Hangfire.BusinessObjects;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire {
    public static class JobSchedulerService {
        public const string PausedJobsSetName = "paused-jobs";
        public static SimpleAction TriggerJob(this (JobSchedulerModule, Frame frame) tuple) 
            => tuple.frame.Action(nameof(TriggerJob)).As<SimpleAction>();
        public static SimpleAction PauseJob(this (JobSchedulerModule, Frame frame) tuple) 
            => tuple.frame.Action(nameof(PauseJob)).As<SimpleAction>();
        public static SimpleAction ResumeJob(this (JobSchedulerModule, Frame frame) tuple) 
            => tuple.frame.Action(nameof(ResumeJob)).As<SimpleAction>();
        
        public static SimpleAction JobDashboard(this (JobSchedulerModule, Frame frame) tuple) 
            => tuple.frame.Action(nameof(JobDashboard)).As<SimpleAction>();

        static readonly ISubject<GenericEventArgs<IObservable<Job>>> CustomJobScheduleSubject=Subject.Synchronize(new Subject<GenericEventArgs<IObservable<Job>>>());
        public static IObservable<GenericEventArgs<IObservable<Job>>> CustomJobSchedule => CustomJobScheduleSubject.AsObservable();

        internal static IObservable<Unit> Connect(this ApplicationModulesManager manager)
            => manager.CheckBlazor(typeof(HangfireStartup).FullName, typeof(JobSchedulerModule).Namespace)
                .Merge(manager.WhenApplication(application => application.ScheduleJobs()
                            .Merge(application.DeleteJobs())
                    )
                    .Merge(manager.TriggerJobsFromAction())
                    .Merge(manager.PauseJobsFromAction())
                    .Merge(manager.ResumeJobsFromAction())
                    .Merge(manager.JobDashboard())
                );

        private static IObservable<Unit> JobDashboard(this ApplicationModulesManager manager)
            => manager.RegisterViewSimpleAction(nameof(JobDashboard), action => {
                    action.TargetObjectType = typeof(JobWorker);
                    action.SelectionDependencyType = SelectionDependencyType.RequireMultipleObjects;
                    action.Caption = action.Caption.Replace("Job", "").Trim();
                })
                .WhenExecuted()
                .SelectMany(args => {
                    var serviceProvider = args.Action.Application.ToBlazor().ServiceProvider;
                    var uri = $"{new Uri($"{serviceProvider.GetService<IHttpContextAccessor>()?.HttpContext.Request.GetDisplayUrl()}").GetLeftPart(UriPartial.Authority)}/hangfire/jobs/details/";
                    var jsRuntime = serviceProvider.GetService<IJSRuntime>();
                    return args.SelectedObjects.Cast<JobWorker>().ToObservable(ImmediateScheduler.Instance).SelectMany(
                            job => jsRuntime?.InvokeAsync<object>("open", new object[] {$"{uri}{job.Id}", "_blank"}).AsTask());

                })
                .ToUnit();

        internal static void ApplyJobState(this BlazorApplication application,ApplyStateContext context,string recurringJobId) {
            using var objectSpace = application.CreateObjectSpace();
            var scheduledJob = objectSpace.GetObjectsQuery<Job>().FirstOrDefault(job1 => job1.Id==recurringJobId);
            if (scheduledJob!=null) {
                var job = objectSpace.EnsureObjectByKey<JobWorker>(context.BackgroundJob.Id);
                if (objectSpace.IsNewObject(job)) {
                    job.Job = scheduledJob;
                }
                var jobState = objectSpace.CreateObject<JobState>();
                jobState.Created = DateTime.Now;
                jobState.State=(WorkerState) Enum.Parse(typeof(WorkerState),context.NewState.Name);
                jobState.Reason = context.NewState.Reason;
                job.Executions.Add(jobState);
                objectSpace.CommitChanges();
                JobStateSubject.OnNext(jobState);
            }
        }

        public static Job Pause(this Job job) {
            using var transaction = JobStorage.Current.GetConnection().CreateWriteTransaction();
            transaction.AddToSet(PausedJobsSetName, job.Id);
            transaction.Commit();
            job.OnChanged(nameof(Job.IsPaused));
            job.ObjectSpace.CommitChanges();
            return job;
        }

        public static Job Resume(this Job job) {
            using var transaction = JobStorage.Current.GetConnection().CreateWriteTransaction();
            transaction.RemoveFromSet(PausedJobsSetName, job.Id);
            transaction.Commit();
            job.OnChanged(nameof(Job.IsPaused));
            job.ObjectSpace.CommitChanges();
            return job;
        }

        public static string RecurringJobId(this IStorageConnection connection,string backgroundJobId) 
            => $"{connection.GetJobParameter(backgroundJobId, "RecurringJobId")}".Replace(@"\", "").Replace(@"""", "");

        private static IObservable<Unit> TriggerJobsFromAction(this ApplicationModulesManager manager)
            => manager.RegisterViewSimpleAction(nameof(TriggerJob), Configure)
                .WhenExecute().SelectMany(args => args.SelectedObjects.Cast<Job>()).Do(job => job.Trigger()).ToUnit();
        
        private static IObservable<Unit> PauseJobsFromAction(this ApplicationModulesManager manager)
            => manager.RegisterViewSimpleAction(nameof(PauseJob), Configure)
                .WhenExecute()
                .SelectMany(args => args.SelectedObjects.Cast<Job>())
                .Do(job => job.Pause())
            .ToUnit();

        private static IObservable<Unit> ResumeJobsFromAction(this ApplicationModulesManager manager)
            => manager.RegisterViewSimpleAction(nameof(ResumeJob), Configure)
                .WhenExecute()
                .SelectMany(args => args.SelectedObjects.Cast<Job>())
                .Do(job => job.Resume())
            .ToUnit();

        private static void Configure(ActionBase action) {
            action.TargetObjectType = typeof(Job);
            action.SelectionDependencyType = SelectionDependencyType.RequireMultipleObjects;
            action.Caption = action.Caption.Replace("Job", "").Trim();
        }

        public static void Trigger(this Job job) 
            => RecurringJob.Trigger(job.Id);

        public static void AddOrUpdateHangfire(this Job job) 
            => RecurringJob.AddOrUpdate(job.Id,job.CallExpression(), () => job.CronExpression.Expression);

        static IObservable<Unit> ScheduleJobs(this XafApplication application) 
            => application.WhenCommitted<Job>(ObjectModification.NewOrUpdated).Objects()
                .SelectMany(scheduledJob => {
                    var args = new GenericEventArgs<IObservable<Job>>(scheduledJob.ReturnObservable());
                    CustomJobScheduleSubject.OnNext(args);
                    if (!args.Handled) {
                        scheduledJob.AddOrUpdateHangfire();
                    }
                    return args.Instance;
                })
                .TraceJobSchedulerModule()
                .ToUnit();

        private static IObservable<Unit> DeleteJobs(this XafApplication application) 
            => application.DeletedObjects<Job>()
                .SelectMany(t => t.objects.Do(job => RecurringJob.RemoveIfExists(job.Id)))
                .TraceJobSchedulerModule().ToUnit();

        static readonly ISubject<JobState> JobStateSubject=Subject.Synchronize(new Subject<JobState>());
        
        public static IObservable<JobState> JobState => JobStateSubject.AsObservable();

        internal static Expression<Action> CallExpression(this Job job) {
            var method = job.JobType.Type.Method(job.JobMethod.Name.Replace(" ", ""));
            var arguments = method.Parameters().Count == 1 && method.Parameters().Any(info => info.ParameterType == typeof(PerformContext))
                ? new Expression[] {Expression.Constant(null, typeof(PerformContext))} : new Expression[0];
            return job.JobType.Type.CallExpression(method,arguments);
        }
        internal static IObservable<TSource> TraceJobSchedulerModule<TSource>(this IObservable<TSource> source, Func<TSource,string> messageFactory=null,string name = null, Action<string> traceAction = null,
            Func<Exception,string> errorMessageFactory=null, ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.All,
            [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0) 
            => source.Trace(name, JobSchedulerModule.TraceSource,messageFactory,errorMessageFactory, traceAction, traceStrategy, memberName);

        internal static IEnumerable<MethodInfo> JobMethods(this AppDomain appDomain) 
            => appDomain.GetAssemblies().FromModelSources().SelectMany(assembly => assembly.JobMethods());

        public static IEnumerable<Assembly> FromModelSources(this IEnumerable<Assembly> assemblies) 
            => assemblies.Where(assembly =>
                CaptionHelper.ApplicationModel.ToReactiveModule<IModelReactiveModulesJobScheduler>().JobScheduler
                    .Sources.Select(source => source.AssemblyName).Contains(assembly.GetName().Name));

        public static IEnumerable<MethodInfo> JobMethods(this Assembly assembly) 
            => assembly.GetTypes()
                .Where(type => type.Attributes<JobProviderAttribute>().Any())
                .Do(type => {
                    if (!type.Constructors().Any(info => info.IsPublic )) {
                        throw new NoDefaultConstructorException(type);
                    }
                })
                .SelectMany(type => type.Methods().Where(info => {
                    var parameterInfos = info.Parameters();
                    return !info.IsSpecialName && info.IsPublic && 
                           (!parameterInfos.Any() || (parameterInfos.Count == 1 && parameterInfos
                               .Any(parameterInfo => parameterInfo.ParameterType == typeof(PerformContext))));
                }));

        public static string JobId(this PerformContext performContext) 
            => performContext.Connection.RecurringJobId(performContext.BackgroundJob.Id);
    }
}
