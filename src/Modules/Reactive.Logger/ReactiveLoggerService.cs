using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base.General;
using DevExpress.Utils;
using Fasterflect;
using JetBrains.Annotations;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.ExpressionExtensions;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.ObjectExtensions;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.Tracing;
using Xpand.Extensions.XAF.ModelExtensions;
using Xpand.Extensions.XAF.ObjectExtensions;
using Xpand.Extensions.XAF.ObjectSpaceExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.XAF.Modules.Reactive.Extensions;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.Reactive.Logger{
    public static class ReactiveLoggerService{
        public static string RXLoggerLogPath{ get; [PublicAPI]set; }=@$"{AppDomain.CurrentDomain.ApplicationPath()}\{AppDomain.CurrentDomain.ApplicationName()}_RXLogger.log";
        private static readonly Subject<ITraceEvent> SavedTraceEventSubject=new();
        public static IObservable<ITraceEvent> ListenerEvents{ get; private set; }
        public static IObservable<ITraceEvent> SavedTraceEvent{ get; }=SavedTraceEventSubject;
        private static ReactiveTraceListener _listener;
        internal static IObservable<Unit> Connect(this ApplicationModulesManager manager) 
            => manager.WhenApplication(application => {
                application.AddNonSecuredType(typeof(TraceEvent));
                _listener ??= new ReactiveTraceListener(application.Title);
                ListenerEvents = _listener.EventTrace.Publish().RefCount();
                return application.BufferUntilCompatibilityChecked(ListenerEvents).SaveEvent(application).ToUnit()
                        .Merge(application.Notifications())
                        .Merge(ListenerEvents.RefreshViewDataSource(application))
                        .Merge(application.RegisterListener(_listener))
                        .Merge(manager.TraceEventListViewDataAccess());
            });

        private static IObservable<Unit> TraceEventListViewDataAccess(this ApplicationModulesManager manager) 
            => manager.WhenGeneratingModelNodes<IModelViews>()
                .Where(views => ((IModelSources)views.Application).Modules.GetPlatform()==Platform.Blazor)
                .SelectMany(views => views.OfType<IModelListView>().Where(view => view.ModelClass.TypeInfo.Type==typeof(TraceEvent))
                    .Do(view => view.DataAccessMode=CollectionSourceDataAccessMode.Queryable).ToNowObservable().ToUnit());

        public static IObservable<Unit> RefreshViewDataSource(this IObservable<ITraceEvent> events, XafApplication application) 
            => application.GetPlatform() == Platform.Web ? Observable.Empty<Unit>()
		        : application.WhenViewOnFrame(typeof(TraceEvent), ViewType.ListView)
                    .SelectMany(frame => events.Throttle(TimeSpan.FromSeconds(1))
				        .TakeUntil(frame.WhenDisposingFrame())
				        .DistinctUntilChanged(_ => _.TraceKey())
                        .ObserveOn(SynchronizationContext.Current)
				        .SelectMany(e => {
					        if (e.Method != nameof(RefreshViewDataSource)){
						        frame?.View?.RefreshDataSource();
						        return e.ReturnObservable();
					        }

					        return Observable.Never<ITraceEvent>();
				        }))
			        .TraceLogger(_ => _.Message)
			        .ToUnit();

        [PublicAPI]
        public static IObservable<TraceEvent> WhenTraceEvent<TLocation>(this XafApplication application, Expression<Func<TLocation, object>> expression, RXAction rxAction = RXAction.All){
            var name = expression.MemberExpressionName();
            return application.WhenTraceEvent(typeof(TLocation), rxAction).Where(_ => _.Method == name);
        }

        [PublicAPI]
        public static IObservable<ITraceEvent> WhenTraceOnNext(this XafApplication application, params string[] methods) 
            => application.WhenTraceOnNext(null, methods);

        public static IObservable<ITraceEvent> WhenTraceOnNext(this XafApplication application, Type location = null,params string[] methods) 
	        => application.WhenTrace(location, RXAction.OnNext, methods);
        
        public static IObservable<ITraceEvent> WhenTraceError(this XafApplication application, Type location = null,params string[] methods) 
	        => application.WhenTrace(location, RXAction.OnError, methods);

        public static IObservable<ITraceEvent> WhenTrace(this XafApplication application, Type location = null,RXAction rxAction = RXAction.All, params string[] methods) 
            => application.Modules.ToTraceSource().SelectMany(t => t.traceSource.Listeners.OfType<ReactiveTraceListener>()).DistinctWith(listener => listener.Name).ToObservable()
		        .SelectMany(listener => listener.EventTrace)
                .When(location, rxAction,methods);

        public static IObservable<ITraceEvent> When(this IObservable<ITraceEvent> source, Type location, RXAction rxAction,params string[] methods) 
            => source.Where(_ => location == null || _.Location == location.Name)
		        .Where(_ => !methods.Any() || methods.Contains(_.Method))
		        .Where(_ => rxAction == RXAction.All || _.RXAction.HasAnyFlag(rxAction));

        [PublicAPI]
        public static IObservable<TraceEvent> WhenTraceOnSubscribeEvent(this XafApplication application, params string[] methods) 
            => application.WhenTraceEvent(null, RXAction.Subscribe, methods);
        
        [PublicAPI]
        public static IObservable<TraceEvent> WhenTraceOnNextEvent(this XafApplication application,params string[] methods) 
            => application.WhenTraceOnNextEvent(null, methods);

        public static IObservable<TraceEvent> WhenTraceOnNextEvent(this XafApplication application, Type location = null,params string[] methods) 
            => application.WhenTraceEvent(location, RXAction.OnNext, methods);

        [PublicAPI]
        public static IObservable<TraceEvent> WhenTraceEvent(this XafApplication application,Type location=null,RXAction rxAction=RXAction.All,params string[] methods) 
            => SavedTraceEvent.When(location, rxAction,methods).Cast<TraceEvent>();

        internal static IObservable<TSource> TraceLogger<TSource>(this IObservable<TSource> source, Func<TSource,string> messageFactory=null,string name = null, Action<string> traceAction = null,
	        Func<Exception,string> errorMessageFactory=null, ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.OnNextOrOnError,
	        [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0) 
            => source.Trace(name, ReactiveLoggerModule.TraceSource,messageFactory,errorMessageFactory, traceAction, traceStrategy, memberName,sourceFilePath,sourceLineNumber);


        public static IEnumerable<(ModuleBase module, TraceSource traceSource)> ToTraceSource(this IEnumerable<ModuleBase> moduleList) 
            => moduleList.SelectMany(m => m.GetType().GetProperties(BindingFlags.Static | BindingFlags.Public)
			        .Where(info => typeof(TraceSource).IsAssignableFrom(info.PropertyType))
			        .Select(info => (module:m,traceSource:(TraceSource)info.GetValue(m)))).Where(o => o.traceSource!=null);


        private static IObservable<Unit> RegisterListener(this XafApplication application, ReactiveTraceListener reactiveTraceListener) 
            => application.Modules.WhenListChanged().SelectMany(_ => _.list.ToTraceSource().ToObservable(Scheduler.Immediate))
                .Do(_ => {
                    if (!_.traceSource.Listeners.Contains(reactiveTraceListener)){
                        _.traceSource.Listeners.Add(reactiveTraceListener);
                    }
                })
                .ToUnit()
                .Merge(application.ApplyModel( reactiveTraceListener));

        private static IObservable<Unit> ApplyModel(this XafApplication application, ReactiveTraceListener reactiveTraceListener) 
            => application.WhenModelChanged()
                .Select(_ => application.Model.ToReactiveModule<IModelReactiveModuleLogger>()?.ReactiveLogger).WhenNotDefault()
                .Do(model => {
                    var modelTraceSourcedModules = model.GetEnabledSources().ToArray();
                    foreach (var module in modelTraceSourcedModules) {
                        var tuples = application.Modules.Where(m => m.Name == module.Id()).ToTraceSource();
                        foreach (var tuple in tuples) {
                            tuple.traceSource.Switch.Level = module.Level;
                            if (!tuple.traceSource.Listeners.Contains(reactiveTraceListener)) {
                                tuple.traceSource.Listeners.Add(reactiveTraceListener);
                            }
                            else {
                                if (module.Level == SourceLevels.Off) {
                                    tuple.traceSource.Listeners.Remove(reactiveTraceListener);
                                }
                            }
                        }
                    }

                    if (!modelTraceSourcedModules.Any()) {
                        var modelTraceSources = model.TraceSources;
                        foreach (var modelTraceSource in modelTraceSources) {
                            var tuples = application.Modules.Where(m => m.Name == modelTraceSource.Id()).ToTraceSource();
                            foreach (var tuple in tuples) {
                                tuple.traceSource.Listeners.Remove(reactiveTraceListener);
                            }
                        }
                    }
                })
                .ToUnit();

        [PublicAPI]
        public static void TraceMessage(this TraceSource traceSource, string value,TraceEventType traceEventType=TraceEventType.Information){
            if (traceSource.Switch.Level != SourceLevels.Off){
                traceSource.Push(new TraceEventMessage {
                    Location = nameof(ReactiveLoggerService), Method = nameof(TraceMessage), RXAction = RXAction.None, Message = value,
                    TraceEventType = traceEventType,Source = traceSource.Name
                });
            }
        }

        [SuppressMessage("ReSharper", "ForCanBeConvertedToForeach")]
        public static void Push(this TraceSource source, string message) {
            var listeneers = source.Listeners.OfType<IPush>().ToArray();
            for (var index = 0; index < listeneers.Length; index++) {
                var listeneer = listeneers[index];
                listeneer.Push(message,source.Name);
            }
        }
        
        [SuppressMessage("ReSharper", "ForCanBeConvertedToForeach")]
        public static void Push(this TraceSource source,TraceEventMessage message) {
            var listeneers = source.Listeners.OfType<IPush>().ToArray();
            for (var index = 0; index < listeneers.Length; index++) {
                var listeneer = listeneers[index];
                listeneer.Push(message);
            }
        }


        public static void TraceEventMessage(this TraceSource traceSource,ITraceEvent traceEvent) 
            => traceSource.TraceEvent(traceEvent.TraceEventType, traceSource.GetHashCode(), $"{traceEvent.Location}.{traceEvent.Method}({traceEvent.Line}): {traceEvent.Action}({traceEvent.Value})");

        private static IObservable<TraceEvent> SaveEvent(this IObservable<ITraceEvent> events, XafApplication application) 
            => application.WhenSetupComplete()
                .Where(_ => {
                    var modelReactiveLogger = application.Model.ToReactiveModule<IModelReactiveModuleLogger>()?.ReactiveLogger;
                    return modelReactiveLogger != null && modelReactiveLogger.GetEnabledSources().Any() ;
                })
                .Select(xafApplication => xafApplication.Model.ToReactiveModule<IModelReactiveModuleLogger>()?.ReactiveLogger.TraceSources)
                .Select(traceSources => (traceSources?.PersistStrategy,traceSources?.PersistStrategyCriteria)).WhenNotDefault()
                .SelectMany(t => events.Where(e => t.PersistStrategy.HasValue && e.Is(t.PersistStrategy.Value)).Pair(t.PersistStrategyCriteria))
                .Buffer(TimeSpan.FromSeconds(3)).WhenNotEmpty()
                .SelectMany(ts => application.ObjectSpaceProvider.NewObjectSpace(space => space.SaveTraceEvent(ts.Select(t => t.source).ToArray(),space.ParseCriteria(ts.First().other))));

        private static bool Is(this ITraceEvent e,ObservableTraceStrategy strategy){
            switch (e.RXAction){
                case RXAction.OnNext when strategy is ObservableTraceStrategy.OnNext or ObservableTraceStrategy.OnNextOrOnError or ObservableTraceStrategy.All:
                case RXAction.OnError when strategy is ObservableTraceStrategy.OnError or ObservableTraceStrategy.OnNextOrOnError or ObservableTraceStrategy.All:
                case RXAction.Dispose when strategy==ObservableTraceStrategy.All :
                case RXAction.Subscribe when strategy==ObservableTraceStrategy.All :
                case RXAction.OnCompleted when strategy==ObservableTraceStrategy.All :
                case RXAction.None when strategy==ObservableTraceStrategy.All :
                    return true;
                default:
                    return false;
            }
        }

        public static IObservable<TraceEvent> SaveTraceEvent(this IObjectSpace objectSpace,
            IList<ITraceEvent> traceEventMessages, CriteriaOperator criteria){
            var lastEvent = objectSpace.GetObjectsQuery<TraceEvent>().OrderByDescending(_ => _.Timestamp).FirstOrDefault();
            foreach (var traceEventMessage in traceEventMessages.Where(e => objectSpace.IsObjectFitForCriteria(criteria,e))){
	            if (lastEvent != null && traceEventMessage.TraceKey() == lastEvent.TraceKey()){
                    lastEvent.Called++;
                    continue;
                }
                var traceEvent = objectSpace.CreateObject<TraceEvent>();
                lastEvent = traceEvent;
                traceEventMessage.MapTo(traceEvent);
            }
            var traceEvents = objectSpace.ModifiedObjects.Cast<TraceEvent>().ToArray();
            objectSpace.CommitChanges();
            return traceEvents.ToNowObservable().Do(traceEvent => SavedTraceEventSubject.OnNext(traceEvent));
        }

        private static IObservable<Unit> Notifications(this XafApplication xafApplication) 
            => xafApplication.WhenSetupComplete().SelectMany(_ => {
                var moduleType = AppDomain.CurrentDomain.GetAssemblyType("DevExpress.ExpressApp.Notifications.NotificationsModule");
                var service = xafApplication.Modules.FindModule(moduleType)?.GetPropertyValue("NotificationsService");
                return service != null ? xafApplication.WhenModelChanged().FirstAsync()
                        .SelectMany(application => {
                            var synchronizationContext = SynchronizationContext.Current;
                            var notifications = application.ToReactiveModule<IModelReactiveModuleLogger>().ReactiveLogger.Notifications;
                            var rules = notifications.Select(notification => (notification.ObjectType.TypeInfo.Type, notification.Criteria,notification.ShowXafMessage,notification.XafMessageType,notification.MessageDisplayInterval)).ToArray();
                            return SavedTraceEvent.Cast<TraceEvent>()
                                .SelectMany(traceEvent => rules.Where(t => traceEvent.ObjectSpace.IsObjectFitForCriteria(CriteriaOperator.Parse(t.Criteria), traceEvent))
                                    .Do(rule => {
                                        var @event = (ISupportNotifications)traceEvent.ObjectSpace.CreateObject(rule.Type);
                                        @event.AlarmTime = DateTime.Now;
                                        @event.GetTypeInfo().FindMember(nameof(ISupportNotifications.NotificationMessage)).SetValue(@event, traceEvent.Value);
                                        traceEvent.ObjectSpace.CommitChanges();
                                    })
                                    .ToNowObservable().ObserveOnContext(synchronizationContext)
                                    .Do(_ => service.CallMethod("Refresh"))
                                    .ShowXafMessage(xafApplication, traceEvent));
                        }).ToUnit()
                    : Observable.Empty<Unit>();
            });

        public static IObservable<(Type objectType, string criteria, bool showXafMessage, InformationType informationType, int messageDisplayInterval)> 
            ShowXafMessage(this IObservable<(Type objectType, string criteria, bool showXafMessage, InformationType informationType
                    , int messageDisplayInterval)> source, XafApplication xafApplication, TraceEvent traceEvent) 
            => source.Where(t => t.showXafMessage).ShowXafMessage(xafApplication, _ => $"{traceEvent.Location}, {traceEvent.Method}, {traceEvent.Value}",t => t.informationType,t => t.messageDisplayInterval);
    }
}