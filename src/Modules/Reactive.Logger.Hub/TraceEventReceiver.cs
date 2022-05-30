using System;
using System.Reactive.Subjects;
using Xpand.Extensions.Tracing;

namespace Xpand.XAF.Modules.Reactive.Logger.Hub{
    public class TraceEventReceiver:ITraceEventHubReceiver{
        static readonly ISubject<ITraceEvent> TraceEventSubject=Subject.Synchronize(new Subject<ITraceEvent>());

        public static IObservable<ITraceEvent> TraceEvent => TraceEventSubject;

        public void OnTraceEvent(TraceEventMessage traceEvent) => TraceEventSubject.OnNext(traceEvent);
    }
}