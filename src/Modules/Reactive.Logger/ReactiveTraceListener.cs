using System;
using System.Diagnostics;
using System.Reactive.Subjects;
using System.Text.RegularExpressions;

using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.Tracing;

namespace Xpand.XAF.Modules.Reactive.Logger {
    
    public class ReactiveTraceListener : RollingFileTraceListener,IPush {
        private readonly string _applicationTitle;
         public static bool DisableFileWriter=true;
        static ReactiveTraceListener() {
            Regex = new Regex(@"(?<Location>[^.]*)\.(?<Method>[^(]*)\((?<Ln>[\d]*)\): (?<Action>[^(]*)\((?<Value>.*)\)",
                RegexOptions.Singleline | RegexOptions.Compiled);
        }

        readonly ISubject<ITraceEvent> _eventTraceSubject = Subject.Synchronize(new Subject<ITraceEvent>());
        
        private static readonly Regex Regex;
        private bool _isDisposed;

        public ReactiveTraceListener(string applicationTitle)  {
            _applicationTitle = applicationTitle;
            TraceOutputOptions = TraceOptions.DateTime;
            Template = "{DateTime:u} {Message}";
        }

        protected override void Dispose(bool disposing) {
            base.Dispose(disposing);
            _isDisposed = true;
        }

        public IObservable<ITraceEvent> EventTrace => _eventTraceSubject.ObserveOnDefault();

        public override void Flush() {
            if (!_isDisposed&&!DisableFileWriter) {
                base.Flush();
            }
        }

        public void Push(ITraceEvent message) {
            message.ApplicationTitle = _applicationTitle;
            message.Value = message.Message;
            PushMessage(message);
        }

        public void Push(string message,string source) {
            var traceEventMessage = new TraceEventMessage {
                CallStack = null,
                DateTime = DateTime.Now,
                Message = $"{message}",
                ProcessId = 0,
                Source = source,
                ThreadId = null,
                Timestamp = DateTime.Now.Ticks,
                TraceEventType = TraceEventType.Information,
                ApplicationTitle = _applicationTitle,
            };
            var match = Regex.Match(traceEventMessage.Message);
            traceEventMessage.Location = match.Groups["Location"].Value;
            traceEventMessage.Method = match.Groups["Method"].Value;
            var matchGroup = match.Groups["Value"];
            traceEventMessage.Value = matchGroup.Success ? matchGroup.Value : traceEventMessage.Message;
            traceEventMessage.Action = match.Groups["Action"].Value;
            if (!string.IsNullOrEmpty(traceEventMessage.Action)) {
                if (Enum.TryParse(traceEventMessage.Action, out RXAction rxAction)) {
                    traceEventMessage.RXAction = rxAction;
                }
            }
            if (traceEventMessage.RXAction == RXAction.OnNext) {
                traceEventMessage.ResultType = traceEventMessage.Method.Substring(traceEventMessage.Method.IndexOf(">", StringComparison.Ordinal) + 1);
                traceEventMessage.Method = traceEventMessage.Method.Substring(0, traceEventMessage.Method.IndexOf(" ", StringComparison.Ordinal));
            }
            var value = match.Groups["Ln"].Value;
            if (!string.IsNullOrEmpty(value)) {
                traceEventMessage.Line = Convert.ToInt32(value);
            }
            PushMessage(traceEventMessage);
        }

        private void PushMessage(ITraceEvent traceEventMessage) {
            _eventTraceSubject.OnNext(traceEventMessage);
            if (!_isDisposed&&!DisableFileWriter) {
                base.WriteTrace(null, traceEventMessage.Source, traceEventMessage.TraceEventType, 0, traceEventMessage.Message, Guid.Empty,null);
            }
        }
    }

}