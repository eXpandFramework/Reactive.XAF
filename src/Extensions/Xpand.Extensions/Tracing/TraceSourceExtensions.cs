using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Xpand.Extensions.Tracing{
    public interface IPush{
        void Push(ITraceEvent message);
        
    }
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

    public static class TraceEventExtensions {
        public static string Key(this ITraceEvent traceEvent) 
            => $"{traceEvent.Location}{traceEvent.Action}{traceEvent.Value}{traceEvent.Source}{traceEvent.Method}{traceEvent.Value}";
    }
    public interface ITraceEvent{
        RXAction RXAction { get; set; }
        string Source{ get; set; }
        TraceEventType TraceEventType{ get; set; }
        string Location{ get; set; }
        string Method{ get; set; }
        int Line{ get; set; }
        string Value{ get; set; }
        string Action{ get; set; }
        string Message{ get; set; }
        string CallStack{ get; set; }
        string LogicalOperationStack{ get; set; }
        DateTime DateTime{ get; set; }
        int ProcessId{ get; set; }
        int Thread{ get; set; }
        long Timestamp{ get; set; }
        string ResultType{ get; set; }    
        string ApplicationTitle{ get; set; }
        
    }

    public static class TraceSourceExtensions {
        [SuppressMessage("ReSharper", "ForCanBeConvertedToForeach")]
        public static void Push(this TraceSource source,ITraceEvent message) {
            var listeneers = source.Listeners.OfType<IPush>().ToArray();
            for (var index = 0; index < listeneers.Length; index++) {
                var listeneer = listeneers[index];
                listeneer.Push(message);
            }
        }

    }
}