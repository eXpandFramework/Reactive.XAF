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
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.Reflection;
using Xpand.Extensions.XAF.Model;
using Xpand.Extensions.XAF.XafApplication;
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
        
        
        private static readonly Subject<ITraceEvent> SavedTraceEventSubject=new Subject<ITraceEvent>();

        public static IObservable<ITraceEvent> ListenerEvents{ get; private set; }

        public static IObservable<ITraceEvent> SavedTraceEvent{ get; }=SavedTraceEventSubject;
        internal static IObservable<Unit> Connect(this ReactiveLoggerModule reactiveLoggerModule){
            
            var application = reactiveLoggerModule.Application;
            if (application == null){
                return Observable.Empty<Unit>();
            }

            var listener = new ReactiveTraceListener(application.Title);
            ListenerEvents = listener.EventTrace.Publish().RefCount();


            return application.BufferUntilCompatibilityChecked(ListenerEvents.Select(_ => _))
                .SaveEvent(application)
                .ToUnit()
                .Merge(ListenerEvents.RefreshViewDataSource(application))
                .Merge(application.RegisterListener(listener),Scheduler.Immediate);
        }


        public static IObservable<Unit> RefreshViewDataSource(this IObservable<ITraceEvent> events, XafApplication application){
            if (application.GetPlatform()==Platform.Web){
                return Observable.Empty<Unit>();
            }
            return application.WhenViewOnFrame(typeof(TraceEvent),ViewType.ListView)
                .SelectMany(frame => {
                    return SynchronizationContext.Current.AsObservable().WhenNotDefault()
                        .SelectMany(context => events.Throttle(TimeSpan.FromSeconds(1))
                            .TakeUntil(frame.WhenDisposingFrame())
                            .DistinctUntilChanged(_ => _.TraceKey())
                            .ObserveOn(context)
                            .SelectMany(e => {
                                if (e.Method != nameof(RefreshViewDataSource)){
                                    frame?.View?.RefreshDataSource();
                                    return e.AsObservable().ToUnit();
                                }

                                return Observable.Never<Unit>();
                            }));
                }).ToUnit()
                .TraceLogger();
        }


        public static IObservable<TraceEvent> WhenMethod(this IObservable<TraceEvent> source, params string[] methods ){
            return source.Where(_ => methods.Contains(_.Method));
        }

        public static IObservable<TraceEvent> WhenTraceEvent<TLocation>(this XafApplication application, Expression<Func<TLocation, object>> expression,
            RXAction rxAction = RXAction.All){
            var name = expression.GetMemberInfo().Name;
            return application.WhenTraceEvent(typeof(TLocation), rxAction).Where(_ => _.Method == name);
        }

        public static IObservable<ITraceEvent> WhenTraceOnNext(this XafApplication application, params string[] methods){
            return application.WhenTraceOnNext(location: null, methods);
        }

        public static IObservable<ITraceEvent> WhenTraceOnNext(this XafApplication application, Type location = null,params string[] methods){
            return application.WhenTrace(location, RXAction.OnNext, methods);
        }

        public static IObservable<ITraceEvent> WhenTrace(this XafApplication application, Type location = null,RXAction rxAction = RXAction.All, params string[] methods){
            return application.Modules.ToTraceSource().ToObservable().SelectMany(_ =>
                _.traceSource.Listeners.OfType<ReactiveTraceListener>().ToObservable().SelectMany(listener => listener.EventTrace))
                .When(location, rxAction,methods);
        }

        public static IObservable<ITraceEvent> When(this IObservable<ITraceEvent> source, Type location,
            RXAction rxAction,params string[] methods){

            return source.Where(_ => location == null || _.Location == location.Name)
                .Where(_ => !methods.Any() || methods.Contains(_.Method))
                .Where(_ => rxAction == RXAction.All || _.RXAction.HasAnyFlag(rxAction));
        }

        public static IObservable<TraceEvent> WhenTraceOnSubscribeEvent(this XafApplication application,
            params string[] methods){
            return application.WhenTraceEvent(null, RXAction.Subscribe, methods);
        }

        public static IObservable<TraceEvent> WhenTraceOnNextEvent(this XafApplication application,params string[] methods){
            return application.WhenTraceOnNextEvent(location: null, methods);
        }

        public static IObservable<TraceEvent> WhenTraceOnNextEvent(this XafApplication application, Type location = null,params string[] methods){
            return application.WhenTraceEvent(location, RXAction.OnNext, methods);
        }

        public static IObservable<TraceEvent> WhenTraceEvent(this XafApplication application,Type location=null,RXAction rxAction=RXAction.All,params string[] methods){
            return SavedTraceEvent.When(location, rxAction,methods).Cast<TraceEvent>();

        }
        internal static IObservable<TSource> TraceLogger<TSource>(this IObservable<TSource> source, string name = null, Action<string> traceAction = null,
            ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.All,[CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0){
            return source.Trace(name,ReactiveLoggerModule.TraceSource,traceAction,traceStrategy,memberName,sourceFilePath,sourceLineNumber);
        }


        public static IEnumerable<(ModuleBase module, TraceSource traceSource)> ToTraceSource(this IEnumerable<ModuleBase> moduleList){
            return moduleList
                .SelectMany(m => m.GetType().GetProperties(BindingFlags.Static | BindingFlags.Public)
                    .Where(info => typeof(TraceSource).IsAssignableFrom(info.PropertyType))
                    .Select(info => (module:m,traceSource:(TraceSource)info.GetValue(m)))).Where(o => o.traceSource!=null);
        }

        private static IObservable<Unit> RegisterListener(this XafApplication application,
            ReactiveTraceListener traceListener){
            var register = application.Modules.WhenListChanged().SelectMany(_ => _.list.ToTraceSource().ToObservable(Scheduler.Immediate))
                .Do(_ => {
                    if (!_.traceSource.Listeners.Contains(traceListener)){
                        _.traceSource.Listeners.Add(traceListener);
                    }
                }).ToUnit();
            var applyModel = application.WhenModelChanged()
                .Select(_ =>application.Model.ToReactiveModule<IModelReactiveModuleLogger>()?.ReactiveLogger).WhenNotDefault()
                .Select(model => model.GetActiveSources())
                .Do(modules => {
                    foreach (var module in modules){
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
                })
                .ToUnit();
            return register.Merge(applyModel);
        }

        internal  static  string ApplicationTitle{ get; set; }

        public static void TraceMessage(this TraceSource traceSource, string value,TraceEventType traceEventType=TraceEventType.Information){
            var traceEventMessage = new TraceEventMessage {
                Location = nameof(ReactiveLoggerService), Method = nameof(TraceMessage), RXAction = RXAction.None, Value = value,
                TraceEventType = traceEventType
            };
            if (traceSource.Switch.Level != SourceLevels.Off){
                traceSource.TraceEventMessage(traceEventMessage);
            }
        }

        public static void TraceEventMessage(this TraceSource traceSource,ITraceEvent traceEvent){
            traceSource.TraceEvent(TraceEventType.Information, traceSource.GetHashCode(), $"{traceEvent.Location}.{traceEvent.Method}({traceEvent.Line}): {traceEvent.Action}({traceEvent.Value})");
        }

        private static IObservable<TraceEvent> SaveEvent(this IObservable<ITraceEvent> events, XafApplication application){
            
            return events.Select(_ => _)
                .Buffer(TimeSpan.FromSeconds(1)).WhenNotEmpty()
                .SelectMany(list => application.ObjectSpaceProvider.ToObjectSpace().SelectMany(space => space.SaveTraceEvent(list)));
        }

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
            var traceEvents = objectSpace.ModifiedObjects.OfType<TraceEvent>()
                .ToArray();
            objectSpace.CommitChanges();
            
            return traceEvents.ToObservable().Do(_ => SavedTraceEventSubject.OnNext(_));
        }

    }
}