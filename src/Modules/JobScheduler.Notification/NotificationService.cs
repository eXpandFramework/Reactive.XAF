using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor;
using Xpand.Extensions.ObjectExtensions;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.Extensions.Reactive.Relay;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.Tracing;
using Xpand.Extensions.TypeExtensions;
using Xpand.Extensions.XAF.Attributes;
using Xpand.Extensions.XAF.ObjectSpaceExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.XAF.Modules.JobScheduler.Hangfire.BusinessObjects;
using Xpand.XAF.Modules.JobScheduler.Hangfire.Notification.BusinessObjects;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire.Notification {
    public static class NotificationService {
        private static readonly ISubject<(JobWorker worker, object[] objects)> NotificationSubject =
            Subject.Synchronize(new Subject<(JobWorker worker, object[] objects)>());

        internal static IObservable<Unit> Connect(this ApplicationModulesManager manager) 
            => manager.GenerateModelNodes()
                .Merge(manager.WhenApplication(application => application.WhenSetupComplete().WhenDefault(_ => application.IsInternal())
                        .SelectMany(_ => application.WhenNotificationJobModification(application.Model.NotificationTypes())))
                    .ToUnit());

        public static IObservable<(JobWorker worker, T[] objects)> WhenNotification<T>(this XafApplication application)
            => application.IsInternal() ? Observable.Empty<(JobWorker worker, T[] objects)>()
                : application.WhenNotification(typeof(T)).Select(t => (t.worker,t.objects.Cast<T>().ToArray()));
        
        [SuppressMessage("ReSharper", "UnusedParameter.Global")]
        public static IObservable<(JobWorker worker, object[] objects)> WhenNotification(this XafApplication application,Type objectType=null) {
            objectType ??= typeof(object);
            return NotificationSubject.Select(e => (e.worker,e.objects.Where(o => objectType.IsInstanceOfType(o)).ToArray()));
        }

        internal static IObservable<Unit> JobNotification(this BlazorApplication application, string workerId) 
            => Observable.Using(() => application.CreateNonSecuredObjectSpace(typeof(JobWorker)),objectSpace => objectSpace.DeferItemResilient(() => {
                var jobWorker = objectSpace.GetObjectsQuery<JobWorker>().FirstOrDefault(job => job.Id==workerId);
                if (jobWorker != null) {
                    var job = ((ObjectStateNotification)jobWorker.Job);
                    var objectType = job.Object.Type;
                    var notificationTypes = application.Model.NotificationTypes();
                    var notificationType = notificationTypes.FirstOrDefault(type => type.Type==objectType);
                    var criteriaOperator = job.NonIndexCriteriaOperator(notificationTypes, objectType);
                    if (notificationType == null) return Observable.Empty<Unit>();
                    job.CreateOrUpdateIndex(notificationTypes);
                    jobWorker.ObjectSpace.CommitChanges();
                    if (ReferenceEquals(criteriaOperator, null)) return Observable.Empty<Unit>();
                    var objects = jobWorker.ObjectSpace.GetObjects(objectType, criteriaOperator).Cast<object>().ToArray();
                    if (!objects.Any()) return Observable.Empty<Unit>();
                    var publish = NotifyWorkerFinished.TakeFirst(id => id==workerId); 
                    if (job.ChainJobs.Any()) {
                        return publish.Select(unit => unit)
                            .ToUnit()
                            .Select(unit => unit)
                            .Merge(Unit.Default.Observe().Do(_ => NotificationSubject.OnNext((jobWorker, objects))).IgnoreElements());
                    }
                    NotificationSubject.OnNext((jobWorker, objects));
                }
                return Observable.Empty<Unit>();

            }));

        private static readonly ISubject<string> NotifyWorkerFinished = Subject.Synchronize(new Subject<string>());
        public static void NotifyFinish(this JobWorker worker) => NotifyWorkerFinished.OnNext(worker.Id);


        private static CriteriaOperator NonIndexCriteriaOperator(this  ObjectStateNotification objectStateNotification,NotificationType[] notificationTypes, Type objectType) {
            var index = objectStateNotification.ObjectSpace.GetObjectsQuery<NotificationJobIndex>()
                .FirstOrDefault(index =>index.ObjectStateNotification.Oid==objectStateNotification.Oid)?.Index;
            return index != null ? CriteriaOperator.And(CriteriaOperator.Parse(objectStateNotification.SelectedObjectsCriteria),
                    CriteriaOperator.Parse($"{notificationTypes.Type(objectType).Member.Name}>?", index)) : null;
        }
        
        private static IObservable<object> WhenNotificationJobModification(this XafApplication application, NotificationType[] modelNotificationTypes) 
            => Observable.Using(() => application.CreateNonSecuredObjectSpace(typeof(ObjectStateNotification)),objectSpace => objectSpace.SaveIndexes(modelNotificationTypes))
                .Merge(application.WhenCommittedDetailed<ObjectStateNotification>(ObjectModification.All)
                    .SelectManyItemResilient(t => application.SaveIndexes(t.details.Select(t1 =>t1.instance ).ToArray(),modelNotificationTypes)
                            .TraceNotificationModule(job => $"{ObjectModification.NewOrUpdated}-{job}")));

        private static IObservable<ObjectStateNotification> SaveIndexes(this XafApplication application,
            ObjectStateNotification[] jobs, NotificationType[] modelNotificationTypes) 
            => Observable.Using(() => application.CreateNonSecuredObjectSpace(typeof(ObjectStateNotification)),objectSpace => jobs.ToNowObservable()
                .DoItemResilient(notificationJob => {
                    notificationJob = objectSpace.GetObject(notificationJob)
                        .CreateOrUpdateIndex(modelNotificationTypes);
                    notificationJob.ObjectSpace.CommitChanges();
                }));

        private static IObservable<ObjectStateNotification> SaveIndexes(this IObjectSpace objectSpace, NotificationType[] modelNotificationTypes) 
            => objectSpace.GetObjectsQuery<ObjectStateNotification>().ToArray()
                .Do(job => job.CreateOrUpdateIndex(modelNotificationTypes))
                .ToArray().ToObservable().Finally(objectSpace.CommitChanges)
                .TraceNotificationModule();

        private static ObjectStateNotification CreateOrUpdateIndex(this ObjectStateNotification job,NotificationType[] modelNotificationTypes) {
            var objectSpace = job.ObjectSpace;
            var lastIndex = objectSpace.Evaluate(job.Object.Type, modelNotificationTypes.Type(job.Object.Type).Member.Name, Aggregate.Max);
            if (lastIndex != null) {
                var jobIndex = objectSpace.GetObjectsQuery<NotificationJobIndex>().FirstOrDefault(index =>index.ObjectStateNotification.Oid==job.Oid) 
                               ?? objectSpace.CreateObject<NotificationJobIndex>();
                jobIndex.Index = Convert.ToInt64(lastIndex);
                jobIndex.ObjectStateNotification=job;
                jobIndex.ObjectType = job.Object.Type;
            }
            return job;
        }

        private static IObservable<Unit> GenerateModelNodes(this ApplicationModulesManager manager) 
            => manager.GenerateModelJobSources().Merge(manager.GenerateModelNotificationTypes());

        private static IObservable<Unit> GenerateModelJobSources(this ApplicationModulesManager manager) 
            => manager.WhenGeneratingModelNodes<IModelJobSchedulerSources>()
                .Do(sources => sources.AddNode<IModelJobSchedulerSource>().AssemblyName = typeof(NotificationService).Assembly.GetName().Name)
                .ToUnit();
        
        private static IObservable<Unit> GenerateModelNotificationTypes(this ApplicationModulesManager manager) 
            => manager.WhenGeneratingModelNodes<IModelNotificationTypes>()
                .SelectMany(sources => sources.Application.BOModel
                    .Where(c =>c.TypeInfo.IsPersistent&& c.TypeInfo.KeyMember != null && c.TypeInfo.KeyMember.MemberType.IsNumeric())
                    .Do(c => {
                        var type = sources.AddNode<IModelNotificationType>();
                        type.Type=c;
                        type.ObjectIndexMember = c.FindMember(c.KeyProperty);
                    }))
                .ToUnit();
    
        internal static IObservable<TSource> TraceNotificationModule<TSource>(this IObservable<TSource> source, Func<TSource,string> messageFactory=null,string name = null, Action<ITraceEvent> traceAction = null,
            Func<Exception,string> errorMessageFactory=null, ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.OnNextOrOnError,Func<string> allMessageFactory = null,
            [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0) 
            => source.Trace(name, JobSchedulerNotificationModule.TraceSource,messageFactory,errorMessageFactory, traceAction, traceStrategy,allMessageFactory, memberName,sourceFilePath,sourceLineNumber);
    }
}