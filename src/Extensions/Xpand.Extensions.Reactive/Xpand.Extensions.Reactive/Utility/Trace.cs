using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using EnumsNET;
using Fasterflect;
using Xpand.Extensions.ExceptionExtensions;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.StringExtensions;
using Xpand.Extensions.Tracing;
using Xpand.Extensions.TypeExtensions;

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


        private static readonly Dictionary<string, RXAction> RXActions = Enums.GetValues<RXAction>()
            .ToDictionary(action => action.AsString(),action => action);

        public static IObservable<TSource> Trace<TSource>(this IObservable<TSource> source, string name = null,
            TraceSource traceSource = null, Func<TSource, string> messageFactory = null, Func<Exception, string> errorMessageFactory = null, Action<ITraceEvent> traceAction = null,
            ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.OnNextOrOnError,Func<string> allMessageFactory = null, string memberName = "",
            string sourceFilePath = "", int sourceLineNumber = 0)
            => Observable.Create<TSource>(observer => {
                if (traceStrategy.Is(ObservableTraceStrategy.All))
                    traceSource.Action("Subscribe", "", traceAction.Push(traceSource), messageFactory,
                        allMessageFactory,errorMessageFactory, memberName, name, sourceFilePath, sourceLineNumber);
                var disposable = source.Subscribe(
                    v => traceSource.OnNext(traceAction, traceStrategy, v, observer, messageFactory,allMessageFactory, 
                        errorMessageFactory, memberName, name, sourceFilePath, sourceLineNumber),
                    e => traceSource.OnError(name, messageFactory,allMessageFactory, errorMessageFactory, traceAction, traceStrategy,
                        memberName, sourceFilePath, sourceLineNumber, e, observer),
                    () => traceSource.OnCompleted(name, messageFactory,allMessageFactory, errorMessageFactory, traceAction, traceStrategy,
                        memberName, sourceFilePath, sourceLineNumber, observer));
                return () => traceSource.Dispose(name, messageFactory,allMessageFactory, errorMessageFactory, traceAction, traceStrategy,
                    memberName, sourceFilePath, sourceLineNumber, disposable);
                });

        private static void Dispose<TSource>(this TraceSource traceSource,string name,  Func<TSource, string> messageFactory,Func<string> allMessageFactory ,
            Func<Exception, string> errorMessageFactory, Action<ITraceEvent> traceAction, ObservableTraceStrategy traceStrategy, string memberName,
            string sourceFilePath, int sourceLineNumber, IDisposable disposable){
            if (traceStrategy.Is(ObservableTraceStrategy.All))
                traceSource.Action("Dispose", "", traceAction.Push(traceSource), messageFactory,allMessageFactory, errorMessageFactory,
                    memberName, name, sourceFilePath, sourceLineNumber);
            disposable.Dispose();
        }

        private static void OnCompleted<TSource>(this TraceSource traceSource,string name,  Func<TSource, string> messageFactory,Func<string> allMessageFactory,
            Func<Exception, string> errorMessageFactory, Action<ITraceEvent> traceAction, ObservableTraceStrategy traceStrategy, string memberName,
            string sourceFilePath, int sourceLineNumber, IObserver<TSource> observer){
            if (traceStrategy.Is(ObservableTraceStrategy.All))
                traceSource.Action("OnCompleted", "", traceAction.Push(traceSource), messageFactory,allMessageFactory, errorMessageFactory,
                    memberName, name, sourceFilePath, sourceLineNumber);
            observer.OnCompleted();
        }

        private static void OnError<TSource>(this TraceSource traceSource,string name,  Func<TSource, string> messageFactory,Func<string> allMessageFactory ,
            Func<Exception, string> errorMessageFactory, Action<ITraceEvent> traceAction, ObservableTraceStrategy traceStrategy, string memberName,
            string sourceFilePath, int sourceLineNumber, Exception e, IObserver<TSource> observer){
            if (traceStrategy.Is(ObservableTraceStrategy.OnError)){
                traceSource.Action("OnError", e, traceAction.TraceError(traceSource), messageFactory,allMessageFactory, errorMessageFactory,
                    memberName, name, sourceFilePath, sourceLineNumber);
            }

            observer.OnError(e);
        }

        private static void OnNext<TSource>(this TraceSource traceSource, Action<ITraceEvent> traceAction,
            ObservableTraceStrategy traceStrategy, TSource v, IObserver<TSource> observer,
            Func<TSource, string> messageFactory,Func<string> allMessageFactory , Func<Exception, string> errorMessageFactory, string memberName,
            string name, string sourceFilePath, int sourceLineNumber){
            if (traceStrategy.Is(ObservableTraceStrategy.OnNext)){
                traceSource.Action("OnNext", v, traceAction.Push(traceSource),messageFactory,allMessageFactory,errorMessageFactory,memberName,name,sourceFilePath,sourceLineNumber);
            }
            observer.OnNext(v);
        }

        private static void Action<TSource>(this TraceSource traceSource, string m, object v, Action<ITraceEvent> ta,
            Func<TSource, string> messageFactory = null,Func<string> allMessageFactory = null, Func<Exception, string> errorMessageFactory = null,
            string memberName = "", string name="",string sourceFilePath = "", int sourceLineNumber = 0) {
            if (traceSource?.Switch.Level == SourceLevels.Off) return;
            string value = null;
            if (v!=null) {
                value = CalculateValue(v, o => new[] {
                            messageFactory?.GetMessageValue(errorMessageFactory, o),
                            allMessageFactory?.GetMessageValue<TSource>(errorMessageFactory, o),
                            errorMessageFactory.GetMessageValue(errorMessageFactory, o)
                        }.Join()).Change<string>();
            }
            var mName = memberName;
            if (m.IsEqualIgnoreCase(nameof(RXAction.OnNext))){
                mName = new[]{memberName," =>",GetSourceName<TSource>()}.JoinString();
            }
 
            var fullValue = AllValues(name, sourceFilePath, sourceLineNumber, mName, m, value).JoinString();
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

        private static string[] AllValues(string name, string sourceFilePath, int sourceLineNumber, string mName, string m, string value) 
            => string.IsNullOrEmpty(name) ? new[] { Path.GetFileNameWithoutExtension(sourceFilePath), ".", mName, "(", sourceLineNumber.ToString(), "): ", m, "(", value, ")" }
                : new[] { name, ".", Path.GetFileNameWithoutExtension(sourceFilePath), ".", mName, "(", sourceLineNumber.ToString(), "): ", m, "(", value, ")" };

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
        private static string GetMessageValue<TSource>(this Func<string> messageFactory, Func<Exception, string> errorMessageFactory, object o){
            try{
                return o switch{
                    TSource => (messageFactory?.Invoke() ?? o).ToString(),
                    Exception e => (errorMessageFactory?.Invoke(e) ?? o).ToString(),
                    _ => messageFactory?.Invoke()
                };
            }
            catch (Exception e){
                return e.Message;
            }
        }

        private static string GetSourceName<TSource>() => 
	        typeof(TSource).IsGenericType ? string.Join(",", typeof(TSource).GetGenericArguments().Select(type => type.Name)) : typeof(TSource).Name;

        private static object CalculateValue(object v, Func<object, string> messageFactory) 
            => messageFactory != null ? messageFactory(v) : v is Exception exception?exception.GetAllInfo():v;

        private static Action<ITraceEvent> TraceError(this Action<ITraceEvent> traceAction, TraceSource traceSource) =>
	        traceAction ?? traceSource.Push;


        private static Action<ITraceEvent> Push(this Action<ITraceEvent> traceAction, TraceSource traceSource) 
            => traceAction ?? traceSource.Push;
    }
    [DebuggerDisplay("{" + nameof(ApplicationTitle) + "}-{" + nameof(Location) + "}-{" + nameof(RXAction) + ("}-{" + nameof(Method) + "}{"+nameof(Value)+"}"))]
    public class TraceEventMessage:ITraceEvent{
        public TraceEventMessage(ITraceEvent traceEvent) => traceEvent.MapProperties(this);

        public TraceEventMessage(){
            
        }

        public override string ToString() => $"{{{ApplicationTitle}}}-{{{Location}}}-{{{RXAction}}}-{{{{{Method}}}}} {{{Value}";

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