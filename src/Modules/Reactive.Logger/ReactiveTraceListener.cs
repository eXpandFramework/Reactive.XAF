using System;
using System.Diagnostics;
using System.Reactive.Subjects;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.Tracing;

namespace Xpand.XAF.Modules.Reactive.Logger {
    public class ReactiveTraceListener : RollingFileTraceListener,IPush {
        private readonly string _applicationTitle;
        [UsedImplicitly] public static bool DisableFileWriter=true;
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

        }


        public void Push(string message,string source) {
            if (!_isDisposed&&!DisableFileWriter) {
                base.WriteTrace(null, message, TraceEventType.Information, 0, message, Guid.Empty,null);
            }
            var traceEvent = new TraceEventMessage {
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

            
            _eventTraceSubject.OnNext(traceEvent);

        }

    }

}