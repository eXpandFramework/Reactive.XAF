using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using DevExpress.ExpressApp;
using DevExpress.Utils;
using JetBrains.Annotations;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.ReflectionExtensions;
using Xpand.Extensions.XAF.ModelExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.XAF.Modules.Reactive.Extensions;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.Reactive.Logger{
    [Flags]
    public enum RXAction{
        None=0,
        Subscribe=2,
        OnNext=4,
        OnCompleted=8,
        Dispose=16,
        OnError=32,
        All=Subscribe|OnNext|OnCompleted|Dispose|OnError
    }
    public static class ReactiveLoggerService{
        public static string RXLoggerLogPath{ get; [PublicAPI]set; }=@$"{AppDomain.CurrentDomain.ApplicationPath()}\{AppDomain.CurrentDomain.SetupInformation.ApplicationName}_RXLogger.log";
        private static readonly Subject<ITraceEvent> SavedTraceEventSubject=new Subject<ITraceEvent>();
        public static IObservable<ITraceEvent> ListenerEvents{ get; private set; }
        public static IObservable<ITraceEvent> SavedTraceEvent{ get; }=SavedTraceEventSubject;
        internal static IObservable<Unit> Connect(this ApplicationModulesManager manager){
	        return manager.WhenApplication(application => {
		        var listener = new ReactiveTraceListener(application.Title);
		        ListenerEvents = listener.EventTrace.Publish().RefCount();
		        return application.BufferUntilCompatibilityChecked(ListenerEvents).Select(_ => _)
			        .SaveEvent(application)
			        .ToUnit()
			        .Merge(ListenerEvents.RefreshViewDataSource(application))
			        .Merge(application.RegisterListener(listener), Scheduler.Immediate);
	        });
        }

        public static IObservable<Unit> RefreshViewDataSource(this IObservable<ITraceEvent> events, XafApplication application) =>
	        application.GetPlatform() == Platform.Web
		        ? Observable.Empty<Unit>()
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
            var name = expression.GetMemberInfo().Name;
            return application.WhenTraceEvent(typeof(TLocation), rxAction).Where(_ => _.Method == name);
        }

        [PublicAPI]
        public static IObservable<ITraceEvent> WhenTraceOnNext(this XafApplication application, params string[] methods) => application
	        .WhenTraceOnNext(null, methods);

        public static IObservable<ITraceEvent> WhenTraceOnNext(this XafApplication application, Type location = null,params string[] methods) => application.WhenTrace(location, RXAction.OnNext, methods);

        public static IObservable<ITraceEvent> WhenTrace(this XafApplication application, Type location = null,RXAction rxAction = RXAction.All, params string[] methods) =>
	        application.Modules.ToTraceSource().SelectMany(_ => _.traceSource.Listeners.OfType<ReactiveTraceListener>()).Distinct().ToObservable()
		        .SelectMany(listener => listener.EventTrace)
		        .Select(_ => _)
		        .When(location, rxAction,methods);

        public static IObservable<ITraceEvent> When(this IObservable<ITraceEvent> source, Type location, RXAction rxAction,params string[] methods) =>
	        source.Where(_ => location == null || _.Location == location.Name)
		        .Where(_ => !methods.Any() || methods.Contains(_.Method))
		        .Where(_ => rxAction == RXAction.All || _.RXAction.HasAnyFlag(rxAction));

        [PublicAPI]
        public static IObservable<TraceEvent> WhenTraceOnSubscribeEvent(this XafApplication application, params string[] methods) =>
	        application.WhenTraceEvent(null, RXAction.Subscribe, methods);
        
        [PublicAPI]
        public static IObservable<TraceEvent> WhenTraceOnNextEvent(this XafApplication application,params string[] methods) => application
	        .WhenTraceOnNextEvent(null, methods);

        public static IObservable<TraceEvent> WhenTraceOnNextEvent(this XafApplication application, Type location = null,params string[] methods) => application
	        .WhenTraceEvent(location, RXAction.OnNext, methods);

        [PublicAPI]
        public static IObservable<TraceEvent> WhenTraceEvent(this XafApplication application,Type location=null,RXAction rxAction=RXAction.All,params string[] methods) => 
	        SavedTraceEvent.When(location, rxAction,methods).Cast<TraceEvent>();

        internal static IObservable<TSource> TraceLogger<TSource>(this IObservable<TSource> source, Func<TSource,string> messageFactory=null,string name = null, Action<string> traceAction = null,
	        Func<Exception,string> errorMessageFactory=null, ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.All,
	        [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0) =>
	        source.Trace(name, ReactiveLoggerModule.TraceSource,messageFactory,errorMessageFactory, traceAction, traceStrategy, memberName,sourceFilePath,sourceLineNumber);


        public static IEnumerable<(ModuleBase module, TraceSource traceSource)> ToTraceSource(this IEnumerable<ModuleBase> moduleList) => moduleList
		        .SelectMany(m => m.GetType().GetProperties(BindingFlags.Static | BindingFlags.Public)
			        .Where(info => typeof(TraceSource).IsAssignableFrom(info.PropertyType))
			        .Select(info => (module:m,traceSource:(TraceSource)info.GetValue(m)))).Where(o => o.traceSource!=null);

        private static IObservable<Unit> RegisterListener(this XafApplication application, ReactiveTraceListener traceListener){
            var register = application.Modules.WhenListChanged().SelectMany(_ => _.list.ToTraceSource().ToObservable(Scheduler.Immediate))
                .Do(_ => {
                    if (!_.traceSource.Listeners.Contains(traceListener)){
                        _.traceSource.Listeners.Add(traceListener);
                    }
                })
                .TakeUntilDisposed(application)
                .Finally(() => traceListener?.Dispose())
                .ToUnit();
            var applyModel = application.WhenModelChanged()
                .Select(_ =>application.Model.ToReactiveModule<IModelReactiveModuleLogger>()?.ReactiveLogger).WhenNotDefault()
                .Do(model => {
	                var modelTraceSourcedModules = model.GetActiveSources().ToArray();
	                foreach (var module in modelTraceSourcedModules){
                        var tuples = application.Modules.Where(m => m.Name == module.Id()).ToTraceSource();
                        foreach (var tuple in tuples){
                            tuple.traceSource.Switch.Level = module.Level;
                            if (!tuple.traceSource.Listeners.Contains(traceListener)){
                                tuple.traceSource.Listeners.Add(traceListener);
                            }
                            else{
                                if (module.Level == SourceLevels.Off){
                                    tuple.traceSource.Listeners.Remove(traceListener);
                                }
                            }
                        }
                    }

                    if (!modelTraceSourcedModules.Any()){
	                    var modelTraceSources = model.TraceSources;
	                    foreach (var modelTraceSource in modelTraceSources){
		                    var tuples = application.Modules.Where(m => m.Name == modelTraceSource.Id()).ToTraceSource();
		                    foreach (var tuple in tuples){
			                    tuple.traceSource.Listeners.Remove(traceListener);
		                    }
	                    }
                    }
                })
                .ToUnit();
            return register.Merge(applyModel);
        }
        
        [PublicAPI]
        public static void TraceMessage(this TraceSource traceSource, string value,TraceEventType traceEventType=TraceEventType.Information){
            if (traceSource.Switch.Level != SourceLevels.Off){
	            var traceEventMessage = new TraceEventMessage {
		            Location = nameof(ReactiveLoggerService), Method = nameof(TraceMessage), RXAction = RXAction.None, Value = value,
		            TraceEventType = traceEventType
	            };
	            traceSource.TraceEventMessage(traceEventMessage);
            }
        }

        public static void TraceEventMessage(this TraceSource traceSource,ITraceEvent traceEvent) => traceSource
	        .TraceEvent(TraceEventType.Information, traceSource.GetHashCode(), $"{traceEvent.Location}.{traceEvent.Method}({traceEvent.Line}): {traceEvent.Action}({traceEvent.Value})");

        private static IObservable<TraceEvent> SaveEvent(this IObservable<ITraceEvent> events, XafApplication application) =>
	        events.Select(_ => _)
		        .Buffer(TimeSpan.FromSeconds(3)).WhenNotEmpty()
		        .Where(list => application.Model.ToReactiveModule<IModelReactiveModuleLogger>().ReactiveLogger.GetActiveSources().Any())
		        .SelectMany(list => application.ObjectSpaceProvider.ToObjectSpace().SelectMany(space => space.SaveTraceEvent(list)));

        public static IObservable<TraceEvent> SaveTraceEvent(this IObjectSpace objectSpace, IList<ITraceEvent> traceEventMessages){
            var lastEvent = objectSpace.GetObjectsQuery<TraceEvent>().OrderByDescending(_ => _.Timestamp).FirstOrDefault();
            foreach (var traceEventMessage in traceEventMessages){
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
            return traceEvents.ToObservable(Scheduler.Immediate)
	            .Do(_ => SavedTraceEventSubject.OnNext(_))
	            ;
        }

    }
}