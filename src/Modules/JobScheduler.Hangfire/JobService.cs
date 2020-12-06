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
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Blazor;
using DevExpress.ExpressApp.Utils;
using Fasterflect;
using Hangfire;
using Hangfire.States;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Xpand.Extensions.Blazor;
using Xpand.Extensions.EventArgExtensions;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.ObjectSpaceExtensions;
using Xpand.XAF.Modules.JobScheduler.Hangfire.BusinessObjects;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire {
    public static class JobService {
        public static SimpleAction TriggerJob(this (JobSchedulerModule, Frame frame) tuple) 
            => tuple.frame.Action(nameof(TriggerJob)).As<SimpleAction>();
        
        public static SimpleAction JobDashboard(this (JobSchedulerModule, Frame frame) tuple) 
            => tuple.frame.Action(nameof(JobDashboard)).As<SimpleAction>();

        static readonly ISubject<GenericEventArgs<IObservable<Job>>> CustomJobScheduleSubject=Subject.Synchronize(new Subject<GenericEventArgs<IObservable<Job>>>());
        public static IObservable<GenericEventArgs<IObservable<Job>>> CustomJobSchedule => CustomJobScheduleSubject.AsObservable();

        internal static IObservable<Unit> Connect(this ApplicationModulesManager manager)
            => manager.CheckBlazor(typeof(JobSchedulerStartup).FullName, typeof(JobSchedulerModule).Namespace)
                .Merge(manager.WhenApplication(application => application.ScheduleJobs()
                            .Merge(application.DeleteJobs()))
                    .Merge(manager.TriggerJobsFromAction())
                    .Merge(manager.JobDashboard())
                );

        private static IObservable<Unit> JobDashboard(this ApplicationModulesManager manager)
            => manager.RegisterViewSimpleAction(nameof(JobDashboard), action => {
                    action.TargetObjectType = typeof(Shooter);
                    action.SelectionDependencyType = SelectionDependencyType.RequireMultipleObjects;
                })
                .WhenExecute()
                .SelectMany(args => {
                    var serviceProvider = args.Action.Application.ToBlazor().ServiceProvider;
                    var uri = $"{new Uri($"{serviceProvider.GetService<IHttpContextAccessor>()?.HttpContext.Request.GetDisplayUrl()}").GetLeftPart(UriPartial.Authority)}/hangfire/jobs/details/";
                    var jsRuntime = serviceProvider.GetService<IJSRuntime>();
                    return args.SelectedObjects.Cast<Shooter>().ToObservable(ImmediateScheduler.Instance).SelectMany(
                            job => jsRuntime?.InvokeAsync<object>("open", new object[] {$"{uri}{job.Id}", "_blank"}).AsTask());

                })
                .ToUnit();

        internal static void ApplyJobState(this BlazorApplication application,ApplyStateContext context,string recurringJobId) {
            using var objectSpace = application.CreateObjectSpace();
            var scheduledJob = objectSpace.GetObjectsQuery<Job>().FirstOrDefault(job1 => job1.Id==recurringJobId);
            if (scheduledJob!=null) {
                var job = objectSpace.EnsureObjectByKey<Shooter>(context.BackgroundJob.Id);
                if (objectSpace.IsNewObject(job)) {
                    job.Job = scheduledJob;
                }
                var jobState = objectSpace.CreateObject<JobState>();
                jobState.Created = DateTime.Now;
                jobState.State=(ScheduledJobState) Enum.Parse(typeof(ScheduledJobState),context.NewState.Name);
                jobState.Reason = context.NewState.Reason;
                job.Executions.Add(jobState);
                objectSpace.CommitChanges();
                JobExecutionSubject.OnNext(jobState);
            }
        }

        private static IObservable<Unit> TriggerJobsFromAction(this ApplicationModulesManager manager)
            => manager.RegisterViewSimpleAction(nameof(TriggerJob), Configure)
                .WhenExecute().SelectMany(args => args.SelectedObjects.Cast<Job>()).Do(job => job.Trigger()).ToUnit();

        private static void Configure(ActionBase action) {
            action.TargetObjectType = typeof(Job);
            action.SelectionDependencyType=SelectionDependencyType.RequireSingleObject;
        }

        public static void Trigger(this Job job) 
            => RecurringJob.Trigger(job.Id);


        public static void AddOrUpdateHangfire(this Job job) 
            => RecurringJob.AddOrUpdate(job.Id,() => job.CallExpression(),() => job.CronExpression.Expression);

        static IObservable<Unit> ScheduleJobs(this XafApplication application) 
            => application.WhenCommited<Job>(ObjectModification.NewOrUpdated).Objects()
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
                .SelectMany(t => t.objects.Cast<Job>().Do(job => RecurringJob.RemoveIfExists(job.Id)))
                .TraceJobSchedulerModule().ToUnit();

        static readonly ISubject<JobState> JobExecutionSubject=Subject.Synchronize(new Subject<JobState>());
        
        public static IObservable<JobState> JobExecution => JobExecutionSubject.AsObservable();

        public static Expression CallExpression(this Job job) {
            var lambdaParser = new NReco.Linq.LambdaParser();
            return lambdaParser.Parse(job.JobExpression.Expression);
            // Console.WriteLine( value ); // --> 5
            // return job.JobExpression.JobType.Type.CallExpression(job.JobMethod.Name.Replace(" ", ""));
        }

        internal static IObservable<TSource> TraceJobSchedulerModule<TSource>(this IObservable<TSource> source, Func<TSource,string> messageFactory=null,string name = null, Action<string> traceAction = null,
            Func<Exception,string> errorMessageFactory=null, ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.All,
            [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0) 
            => source.Trace(name, JobSchedulerModule.TraceSource,messageFactory,errorMessageFactory, traceAction, traceStrategy, memberName);

        internal static IEnumerable<MethodInfo> JobMethods(this AppDomain appDomain) 
            => appDomain.GetAssemblies().Where(assembly =>
                    CaptionHelper.ApplicationModel.ToReactiveModule<IModelReactiveModulesJobScheduler>().JobScheduler
                        .Sources.Select(source => source.AssemblyName).Contains(assembly.GetName().Name))
                .SelectMany(assembly => assembly.GetTypes()
                    .Where(type => type.Attributes<JobProviderAttribute>().Any()&&type.Constructors().Any(info => !info.Parameters().Any())))
                .SelectMany(type => type.Methods().Where(info => info.Attributes<JobProviderAttribute>().Any()&&!info.Parameters().Any()));
        
    }
}
