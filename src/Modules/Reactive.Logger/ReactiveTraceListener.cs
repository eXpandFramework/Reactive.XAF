using System;
using System.Diagnostics;
using System.Reactive.Subjects;
using System.Text.RegularExpressions;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.Tracing;

namespace Xpand.XAF.Modules.Reactive.Logger {
    public class ReactiveTraceListener : RollingFileTraceListener {
        private readonly string _applicationTitle;
        public static bool DisableFileWriter;
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

        protected override void WriteTrace(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message,
            Guid? relatedActivityId, object[] data) {
            if (!_isDisposed&&!DisableFileWriter) {
                base.WriteTrace(eventCache, message, eventType, id, message, relatedActivityId, data);
            }
            var traceEvent = new TraceEventMessage {
                CallStack = eventCache.Callstack,
                DateTime = eventCache.DateTime,
                Message = $"{message}",
                ProcessId = eventCache.ProcessId,
                Source = source,
                ThreadId = eventCache.ThreadId,
                Timestamp = eventCache.Timestamp,
                TraceEventType = eventType,
                ApplicationTitle = _applicationTitle,
            };

            var match = Regex.Match(traceEvent.Message);
            traceEvent.Location = match.Groups["Location"].Value;
            traceEvent.Method = match.Groups["Method"].Value;
            traceEvent.Value = match.Groups["Value"].Value;
            traceEvent.Action = match.Groups["Action"].Value;
            if (!string.IsNullOrEmpty(traceEvent.Action)) {
                if (Enum.TryParse(traceEvent.Action, out RXAction rxAction)) {
                    traceEvent.RXAction = rxAction;
                }
            }

            if (traceEvent.RXAction == RXAction.OnNext) {
                traceEvent.ResultType =
                    traceEvent.Method.Substring(traceEvent.Method.IndexOf(">", StringComparison.Ordinal) + 1);
                traceEvent.Method =
                    traceEvent.Method.Substring(0, traceEvent.Method.IndexOf(" ", StringComparison.Ordinal));
            }

            var value = match.Groups["Ln"].Value;
            if (!string.IsNullOrEmpty(value)) {
                traceEvent.Line = Convert.ToInt32(value);
            }

            traceEvent.LogicalOperationStack =
                string.Join(Environment.NewLine, eventCache.LogicalOperationStack.ToArray());
            _eventTraceSubject.OnNext(traceEvent);

        }

    }

}