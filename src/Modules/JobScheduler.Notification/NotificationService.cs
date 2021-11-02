using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor;
using Xpand.Extensions.Blazor;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.TypeExtensions;
using Xpand.Extensions.XAF.ObjectSpaceExtensions;
using Xpand.XAF.Modules.JobScheduler.Hangfire.Notification.BusinessObjects;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire.Notification {
    public static class NotificationService {
        private static readonly ISubject<(ObjectStateNotification job, object[] objects)> NotificationSubject =
            Subject.Synchronize(new Subject<(ObjectStateNotification job, object[] objects)>());

        internal static IObservable<Unit> Connect(this ApplicationModulesManager manager) 
            => manager.GenerateModelNodes()
                .Merge(manager.WhenApplication(application => application.WhenSetupComplete()
                        .WhenDefault(_ => ((ISharedBlazorApplication)application).UseNonSecuredObjectSpaceProvider)
                        .SelectMany(_ => application.WhenNotificationJobModification(application.Model.NotificationTypes())))
                    .ToUnit());

        public static IObservable<(ObjectStateNotification job, T[] objects)> WhenNotification<T>(this XafApplication application) 
            => application.WhenNotification(typeof(T)).Select(t => (t.job,t.objects.Cast<T>().ToArray())).AsObservable();
        
        public static IObservable<(ObjectStateNotification job, object[] objects)> WhenNotification(this XafApplication application,Type objectType=null) {
            objectType ??= typeof(object);
            return NotificationSubject.Select(t =>(t.job,t.objects.Where(o => objectType.IsInstanceOfType(o)).ToArray()) )
                .AsObservable().TraceNotificationModule(t => t.job.Object.Name);
        }

        internal static IObservable<Unit> JobNotification(this BlazorApplication application, string notificationJobId) 
            => Observable.Using(application.CreateObjectSpace, objectSpace => {
                var notificationJob = objectSpace.GetObjectsQuery<ObjectStateNotification>().FirstOrDefault(job => job.Id==notificationJobId);
                if (notificationJob != null) {
                    var objectType = notificationJob.Object.Type;
                    var notificationTypes = application.Model.NotificationTypes();
                    var notificationType = notificationTypes.FirstOrDefault(type => type.Type==objectType);
                    var criteriaOperator = notificationJob.NonIndexCriteriaOperator(notificationTypes, objectType);
                    if (notificationType!=null) {
                        notificationJob.CreateOrUpdateIndex(notificationTypes);
                        notificationJob.ObjectSpace.CommitChanges();
                        if (!ReferenceEquals(criteriaOperator, null)) {
                            var objects = notificationJob.ObjectSpace.GetObjects(objectType, criteriaOperator).Cast<object>().ToArray();
                            if (objects.Any()) {
                                NotificationSubject.OnNext((notificationJob, objects));
                            }
                            return objects.ToNowObservable().ToUnit();
                        }
                    }
                }
                return Observable.Empty<Unit>();
            });
        
        private static CriteriaOperator NonIndexCriteriaOperator(this  ObjectStateNotification objectStateNotification,NotificationType[] notificationTypes, Type objectType) {
            var index = objectStateNotification.ObjectSpace.GetObjectsQuery<NotificationJobIndex>()
                .FirstOrDefault(index =>index.ObjectStateNotification.Oid==objectStateNotification.Oid)?.Index;
            return index != null ? CriteriaOperator.And(CriteriaOperator.Parse(objectStateNotification.SelectedObjectsCriteria),
                    CriteriaOperator.Parse($"{notificationTypes.Type(objectType).Member.Name}>?", index)) : null;
        }
        
        private static IObservable<object> WhenNotificationJobModification(this XafApplication application, NotificationType[] modelNotificationTypes) 
            => Observable.Using(application.CreateObjectSpace,objectSpace => objectSpace.SaveIndexes(modelNotificationTypes))
                .Merge(application.WhenCommitedDetailed<ObjectStateNotification>(ObjectModification.NewOrUpdated)
                    .SelectMany(t => application.SaveIndexes(t.details.Select(t1 =>t1.instance ).ToArray(),modelNotificationTypes)
                            .TraceNotificationModule(job => $"{ObjectModification.NewOrUpdated}-{job}")));

        private static IObservable<ObjectStateNotification> SaveIndexes(this XafApplication application,
            ObjectStateNotification[] jobs, NotificationType[] modelNotificationTypes) 
            => Observable.Using(application.CreateObjectSpace, objectSpace => jobs.Do(notificationJob => {
                notificationJob = objectSpace.GetObject(notificationJob)
                    .CreateOrUpdateIndex(modelNotificationTypes);
                notificationJob.ObjectSpace.CommitChanges();
            }).ToNowObservable());

        private static IObservable<ObjectStateNotification> SaveIndexes(this IObjectSpace objectSpace, NotificationType[] modelNotificationTypes) 
            => objectSpace.GetObjectsQuery<ObjectStateNotification>().ToArray()
                .Do(job => job.CreateOrUpdateIndex(modelNotificationTypes))
                .Finally(objectSpace.CommitChanges)
                .ToArray().ToObservable().TraceNotificationModule();

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
    
        internal static IObservable<TSource> TraceNotificationModule<TSource>(this IObservable<TSource> source, Func<TSource,string> messageFactory=null,string name = null, Action<string> traceAction = null,
            Func<Exception,string> errorMessageFactory=null, ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.All,
            [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0) 
            => source.Trace(name, JobSchedulerNotificationModule.TraceSource,messageFactory,errorMessageFactory, traceAction, traceStrategy, memberName,sourceFilePath,sourceLineNumber);
    }
}