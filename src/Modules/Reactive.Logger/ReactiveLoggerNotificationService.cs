using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Notifications;
using DevExpress.Persistent.Base.General;
using Fasterflect;
using Microsoft.Extensions.Caching.Memory;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.MemoryCacheExtensions;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.ErrorHandling;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.Tracing;
using Xpand.Extensions.XAF.ObjectExtensions;
using Xpand.Extensions.XAF.ObjectSpaceExtensions;
using Xpand.XAF.Modules.Reactive.Services;
using static Xpand.XAF.Modules.Reactive.Logger.ReactiveLoggerService;

namespace Xpand.XAF.Modules.Reactive.Logger{
    public static class ReactiveLoggerNotificationService {
        
        internal static IObservable<Unit> Notifications(this XafApplication application) 
            => application.WhenSetupComplete()
                .SelectMany(_ => {
                    var service = application.NotificationsService();
                    return service == null ? Observable.Empty<Unit>() : application.Rules().Observe().NotifyRules(application)
                        .WithLatestFrom(application.WhenSynchronizationContext(),(_, context) 
                            => context.DeferAction(() => context.Post(_ => service.Refresh(),service)))
                        .Merge()
                        .MergeToUnit(application.NotifySystemExceptions());
                });

        private static IObservable<Unit> NotifySystemExceptions(this XafApplication application) 
            => application.Model.ToReactiveModule<IModelReactiveModuleLogger>().ReactiveLogger.Notifications.Observe()
                .MergeIgnored(notifications => application.WhenMainWindowCreated().ToController("DevExpress.ExpressApp.Validation.Win.ValidationResultsShowingController")
                    .Do(controller => controller.Active[nameof(NotifySystemExceptions)]=!notifications.DisableValidationResults))
                .SelectMany(application.NotifySystemExceptions);


        private static readonly IMemoryCache Seen = new MemoryCache(new MemoryCacheOptions { SizeLimit = 10000 });

        private static IObservable<Unit> NotifySystemExceptions(this XafApplication application, IModelReactiveLoggerNotifications notifications) 
            => !notifications.NotifySystemException ? Observable.Empty<Unit>() : application.WhenWin()
                .WhenCustomHandleException().Do(t => t.handledEventArgs.Handled = true)
                .Where(t => {
                    var correlationId = t.exception.CorrelationId();
                    return !t.exception.IsSkipped() &&
                           (correlationId == null || Seen.TryAdd(correlationId.Value));
                })
                .SelectMany(t => t.originalException.Observe().SelectMany(exception => exception.Throw<Unit>())
                    .TraceErrorLogger().CompleteOnError().WhenCompleted())
                .ToUnit();

        public static NotificationsService NotificationsService(this XafApplication application) 
            => (NotificationsService)application.Modules.FindModule(
                    AppDomain.CurrentDomain.GetAssemblyType("DevExpress.ExpressApp.Notifications.NotificationsModule"))
                ?.GetPropertyValue("NotificationsService");

        public static IObservable<Unit> NotifyRules(this IObservable<(Type Type, string Criteria, bool ShowXafMessage, InformationType XafMessageType, int MessageDisplayInterval)[]> source,XafApplication application) 
            => source.SelectMany(rules => SavedTraceEventSubject.Cast<TraceEvent>()
                    .SelectMany(traceEvent => rules.Where(t => traceEvent.Match(t.Criteria)).Select(rule => (rule, traceEvent))
                    .Select(t => {
                        var supportNotifications = (ISupportNotifications)traceEvent.ObjectSpace.CreateObject(t.rule.Type);
                        supportNotifications.AlarmTime = DateTime.Now;
                        var value = traceEvent.FilterInternals( new[]{traceEvent.Location,traceEvent.Method,traceEvent.Value}.WhereNotEmpty().JoinCommaSpace());
                        supportNotifications.GetTypeInfo().FindMember(nameof(ISupportNotifications.NotificationMessage)).SetValue(supportNotifications, value);
                        traceEvent.ObjectSpace.CommitChanges();
                        return (supportNotifications, t.rule);
                    })
                    .ToNowObservable()
                    .ShowXafMessage(application, traceEvent))
                )
                .ToUnit()
            ;

        internal static string FilterInternals(this ITraceEvent traceEvent, string value) 
            => traceEvent.Location != nameof(ReactiveLoggerNotificationService) || traceEvent.Method != nameof(NotifySystemExceptions) ? value : traceEvent.Value;

        private static (Type Type, string Criteria, bool ShowXafMessage, InformationType XafMessageType, int MessageDisplayInterval)[] Rules(this XafApplication application) 
            => application.Model.ToReactiveModule<IModelReactiveModuleLogger>().ReactiveLogger.Notifications
                .Select(notification => (notification.ObjectType.TypeInfo.Type, notification.Criteria,notification.ShowXafMessage,notification.XafMessageType,notification.MessageDisplayInterval)).ToArray();


    }
}