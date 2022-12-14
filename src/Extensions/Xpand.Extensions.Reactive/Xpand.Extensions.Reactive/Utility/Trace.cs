using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using Fasterflect;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Tracing;

namespace Xpand.Extensions.Reactive.Utility{
    public enum ObservableTraceStrategy{
        None,
        OnNext,
        OnError,
        OnNextOrOnError,
        All,
        Default,
    }

    public static partial class Utility{
        private static ConcurrentDictionary<Type, Func<object, string>> Serialization{ get; }
        private static readonly Random Random;

        public static Func<object, string> Serializer = o => o.ToString();
        static Utility() {
            Serialization = new ConcurrentDictionary<Type, Func<object, string>>();
            Random = new Random((int) DateTime.Now.Ticks);
        }

        public static bool AddTraceSerialization(Type type) => Serialization.TryAdd(type, Serializer);

        public static bool AddTraceSerialization<T>(Func<T,string> function) => Serialization.TryAdd(typeof(T), o => function((T) o));


        private static readonly Dictionary<string, RXAction> RXActions = Enum.GetValues(typeof(RXAction)).Cast<RXAction>()
            .ToDictionary(action => action.ToString(), action => action);
        
        public static IObservable<TSource> Trace<TSource>(this IObservable<TSource> source, string name = null, TraceSource traceSource = null,
            Func<TSource, string> messageFactory = null, Func<Exception, string> errorMessageFactory = null, Action<ITraceEvent> traceAction = null,
            ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.OnNextOrOnError, string memberName = "", string sourceFilePath = "", int sourceLineNumber = 0)
            => Observable.Create<TSource>(observer => { 
                void Action(string m, object v, Action<ITraceEvent> ta){
                    if (traceSource?.Switch.Level == SourceLevels.Off){
                        return;
                    }
                    string value = null;
                    if (v!=null){
                        value = $@"{CalculateValue(v, o => messageFactory.GetMessageValue( errorMessageFactory, o))}";
                    }
                    var mName = memberName;
                    if (m == "OnNext"){
                        mName = $"{memberName} =>{GetSourceName<TSource>()}";
                    }
                    var fullValue = $"{name}.{Path.GetFileNameWithoutExtension(sourceFilePath)}.{mName}({sourceLineNumber.ToString()}): {m}({value})".TrimStart('.');
                    var traceEventMessage = new TraceEventMessage() {
                        Action = m, RXAction = RXActions[m], Line = sourceLineNumber, DateTime = DateTime.Now,
                        Method = mName, Location = Path.GetFileNameWithoutExtension(sourceFilePath), Source = traceSource?.Name,
                        Value = fullValue,SourceFilePath=sourceFilePath, Message = value??fullValue,
                        Timestamp =DateTime.Now.Ticks,Thread = Thread.CurrentThread.ManagedThreadId,TraceEventType =TraceEventType.Information 
                    };
                    if (traceEventMessage.RXAction == RXAction.OnNext) {
                        traceEventMessage.ResultType = traceEventMessage.Method.Substring(traceEventMessage.Method.IndexOf(">", StringComparison.Ordinal) + 1);
                        traceEventMessage.Method = traceEventMessage.Method.Substring(0, traceEventMessage.Method.IndexOf(" ", StringComparison.Ordinal));
                    }
                    ta(traceEventMessage);
                }
                if (traceStrategy.Is(ObservableTraceStrategy.All))
                    Action("Subscribe", "", traceAction.Push(traceSource));
                
                var disposable = source.Subscribe(
                    v => {
                        if (traceStrategy.Is(ObservableTraceStrategy.OnNext)){
                            Action("OnNext", v, traceAction.Push(traceSource));
                        }
                        observer.OnNext(v);
                    },
                    e => {
                        if (traceStrategy.Is(ObservableTraceStrategy.OnError)) {
                            Action("OnError", e, traceAction.TraceError(traceSource));
                        }
                        observer.OnError(e);
                    },
                    () => {
                        if (traceStrategy.Is(ObservableTraceStrategy.All))
                            Action("OnCompleted", "", traceAction.Push(traceSource));
                        observer.OnCompleted();
                    });
                return () => {
                    if (traceStrategy.Is(ObservableTraceStrategy.All))
                        Action("Dispose", "", traceAction.Push(traceSource));
                    disposable.Dispose();
                };
        });

        public static bool Is(this ObservableTraceStrategy source,ObservableTraceStrategy target) 
            => source == ObservableTraceStrategy.All || source switch {
                ObservableTraceStrategy.OnNext => new[]
                    { ObservableTraceStrategy.OnNext, ObservableTraceStrategy.Default,ObservableTraceStrategy.OnNextOrOnError }.Contains(target),
                ObservableTraceStrategy.OnError => new[]
                    { ObservableTraceStrategy.OnError, ObservableTraceStrategy.Default,ObservableTraceStrategy.OnNextOrOnError }.Contains(target),
                ObservableTraceStrategy.None => new[] { ObservableTraceStrategy.None }.Contains(target),
                ObservableTraceStrategy.OnNextOrOnError => new []{ObservableTraceStrategy.OnNext,ObservableTraceStrategy.OnError, ObservableTraceStrategy.Default,ObservableTraceStrategy.All }.Contains(target),
                _ => throw new NotImplementedException()
            };

        private static string GetMessageValue<TSource>(this Func<TSource, string> messageFactory, Func<Exception, string> errorMessageFactory, object o){
            try{
                return o switch{
                    TSource t => (messageFactory?.Invoke(t) ?? o).ToString(),
                    Exception e => (errorMessageFactory?.Invoke(e) ?? o).ToString(),
                    _ => o.ToString()
                };
            }
            catch (Exception e){
                return e.Message;
            }
        }

        private static string GetSourceName<TSource>() => 
	        typeof(TSource).IsGenericType ? string.Join(",", typeof(TSource).GetGenericArguments().Select(type => type.Name)) : typeof(TSource).Name;

        private static object CalculateValue(object v, Func<object, string> messageFactory) =>
            messageFactory != null ? messageFactory(v) : v.GetType().FromHierarchy(_ => _.BaseType)
                .Select(_ => Serialization.TryGetValue(_, out var func) ? func(v) : null).FirstOrDefault()?? v;

        private static Action<ITraceEvent> TraceError(this Action<ITraceEvent> traceAction, TraceSource traceSource) =>
	        traceAction ?? (s => {
		        if (traceSource != null){
                    traceSource.Push(s);
		        }
		        else{
                    throw new NotImplementedException();
			        // System.Diagnostics.Trace.TraceError(s);
		        }
	        });


        private static Action<ITraceEvent> Push(this Action<ITraceEvent> traceAction, TraceSource traceSource) 
            => traceAction ?? (s => {
		        if (traceSource != null){
			        traceSource.Push(s);
		        }
		        else{
                    throw new NotImplementedException();
			        // System.Diagnostics.Trace.TraceInformation(s);
		        }

	        });
    }
    [DebuggerDisplay("{" + nameof(ApplicationTitle) + "}-{" + nameof(Location) + "}-{" + nameof(RXAction) + ("}-{" + nameof(Method) + "}{"+nameof(Value)+"}"))]
    public class TraceEventMessage:ITraceEvent{
        public TraceEventMessage(ITraceEvent traceEvent) => traceEvent.MapProperties(this);

        public TraceEventMessage(){
            
        }

        public string ApplicationTitle{ get; set; }
        public string Source{ get; set; }
        public TraceEventType TraceEventType{ get; set; }
        public string Location{ get; set; }
        public string Method{ get; set; }
        public int Line{ get; set; }
        public string Value{ get; set; }
        public string Action{ get; set; }
        public RXAction RXAction{ get; set; }
        public string Message{ get; set; }
        public string CallStack{ get; set; }
        public string LogicalOperationStack{ get; set; }
        public DateTime DateTime{ get; set; }
        public int ProcessId{ get; set; }
        public int Thread{ get; set; }
        public long Timestamp{ get; set; }
        public string ResultType{ get; set; }
        public string SourceFilePath { get; set; }
    }



}