using System;
using System.Diagnostics;
using System.Reactive.Subjects;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.Tracing;

namespace Xpand.XAF.Modules.Reactive.Logger {
    
    public class ReactiveTraceListener : RollingFileTraceListener,IPush {
        
         public static readonly bool DisableFileWriter=true;
         readonly ISubject<ITraceEvent> _eventTraceSubject = Subject.Synchronize(new Subject<ITraceEvent>());
         private bool _isDisposed;

        public ReactiveTraceListener()  {
            TraceOutputOptions = TraceOptions.DateTime;
            Template = "{DateTime:u} {Message}";
        }

        public string Title { get; internal set; }
        protected override void Dispose(bool disposing) {
            base.Dispose(disposing);
            _isDisposed = true;
        }

        public IObservable<ITraceEvent> EventTrace => _eventTraceSubject.ObserveOnDefault();

        public override void Flush() {
            if (_isDisposed || DisableFileWriter) return;
            base.Flush();
        }

        public void Push(ITraceEvent message) {
            message.ApplicationTitle = Title;
            message.Value = message.Message;
            PushMessage(message);
        }
        
        private void PushMessage(ITraceEvent traceEventMessage) {
            _eventTraceSubject.OnNext(traceEventMessage);
            if (_isDisposed || DisableFileWriter) return;
            base.WriteTrace(null, traceEventMessage.Source, traceEventMessage.TraceEventType, 0, traceEventMessage.Message, Guid.Empty,null);
        }
    }

}