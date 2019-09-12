using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using DevExpress.ExpressApp;
using DevExpress.Utils;
using Xpand.Source.Extensions.System.Refelction;
using Xpand.Source.Extensions.XAF.Model;
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
        All=Subscribe|OnNext|OnCompleted|Dispose
    }
    public static class ReactiveLoggerService{
        
        
        private static readonly Subject<ITraceEvent> SavedTraceEventSubject=new Subject<ITraceEvent>();

        public static IObservable<ReactiveTraceListener> RegisteredListener{get; private set; }


//        private static IObservable<ReactiveTraceListener> _registerListener;
        public static IObservable<ITraceEvent> ListenerEvents{ get; private set; }

        public static IObservable<ITraceEvent> SavedTraceEvent{ get; }=SavedTraceEventSubject;
        internal static IObservable<Unit> Connect(this ReactiveLoggerModule reactiveLoggerModule){

            var application = reactiveLoggerModule.Application;
            if (application == null){
                return Observable.Empty<Unit>();
            }

            var listener = new ReactiveTraceListener(application.Title);
            
            
            ListenerEvents = listener.EventTrace.Publish().RefCount();
            
            
            
//                .SelectMany(listener => application.BufferUntilCompatibilityChecked(listener.EventTrace.Select(_ => _)))
//                .Publish().RefCount()
//                .Select(_ => _);
            var registeredListener = application.RegisterListener(listener)
                    .TakeUntil(application.WhenDisposed())
                    .Replay(1);
            registeredListener.Connect();
            RegisteredListener = registeredListener;
            return application.BufferUntilCompatibilityChecked1(ListenerEvents.Select(_ => _))
                .SaveEvent(application)
                .ToUnit()
                .Merge(RegisteredListener.ToUnit())
                .Merge(ListenerEvents.RefreshViewDataSource(application))
                .Do(unit => {},() => {})
                .ToUnit();
                
//                .Merge(startMessage)
        }

        public static IObservable<Unit> RefreshViewDataSource(this IObservable<ITraceEvent> events, XafApplication application){
            return application.WhenViewOnFrame(typeof(TraceEvent),ViewType.ListView)
                .SelectMany(frame => {
                    var synchronizationContext = SynchronizationContext.Current;
                    return events.Throttle(TimeSpan.FromSeconds(1))
                        .TakeUntil(frame.WhenDisposingFrame())
                        .ObserveOn(synchronizationContext) 
                        .SelectMany(e => {
                            if (e.Method != nameof(RefreshViewDataSource)){
                                frame?.View.RefreshDataSource();
                                return e.AsObservable().ToUnit();
                            }

                            return Observable.Never<Unit>();
                        });
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

        public static IObservable<ITraceEvent> WhenTrace(this XafApplication application, Type location = null,
            RXAction rxAction = RXAction.All, params string[] methods){
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

        private static IObservable<ReactiveTraceListener> RegisterListener(this XafApplication application,
            ReactiveTraceListener traceListener){
            
            return application.ReactiveModulesModel().ReactiveLogger()
                    .Select(logger => logger)
                    .Select(model => model.GetActiveSources())
                    .Do(modules => {
                        foreach (var module in modules){
                            var tuples = application.Modules.Where(m => m.Name==module.Id()).ToTraceSource();
                            foreach (var tuple in tuples){
                                tuple.traceSource.Switch.Level=module.Level;
                                if (module.Level!=SourceLevels.Off){
                                    if (!tuple.traceSource.Listeners.Contains(traceListener)){
                                        tuple.traceSource.Listeners.Add(traceListener);
                                    }
                                }
                            }    
                        }
                        
                    })
                .To(traceListener)
                .IgnoreElements()
                .Merge(traceListener.AsObservable())
                .Select(listener => listener)
                ;
        }
//        static readonly ISubject<ITraceEvent> TraceEventSubject=new Subject<ITraceEvent>();
        internal  static  string ApplicationTitle{ get; set; }
//        public static IObservable<ITraceEvent> TraceEvent => TraceEventSubject.CountSubsequent(_ => _.TraceKey()).UpdateTraceCalls();

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
//                .Select((_, i) => {
//                    if (i == 0){
//                        var activeSources = application.Model.ToReactiveModules<IModelReactiveModuleLogger>().ReactiveLogger.GetActiveSources().Select(module => module.Id()).ToArray();
//                        foreach (var valueTuple in application.Modules.ToTraceSource().Where(tuple => activeSources.Contains(tuple.traceSource.Name))){
//                            valueTuple.traceSource.TraceMessage(StartMessage);
//                        }
//                    }
//
//                    return _;
//                })
//                .Buffer(TimeSpan.FromSeconds(2)).WhenNotEmpty()
                .SelectMany(list => application.ObjectSpaceProvider.ToObjectSpace().SelectMany(space => space.SaveTraceEvent(new[]{list})));
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