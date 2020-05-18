using System;
using System.Diagnostics;
using System.IO;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.RegularExpressions;

namespace Xpand.XAF.Modules.Reactive.Logger{
    public class ReactiveTraceListener : TextWriterTraceListener{
        private readonly string _applicationTitle;
        
        readonly ISubject<ITraceEvent> _eventTraceSubject=Subject.Synchronize(new Subject<ITraceEvent>());
        private static Lazy<FileStream> _stream = NewStream();

        private static Lazy<FileStream> NewStream(){
            return new Lazy<FileStream>(() => File.Open(ReactiveLoggerService.RXLoggerLogPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite));
        }

        public ReactiveTraceListener(string applicationTitle) : base(_stream.Value){
            _applicationTitle = applicationTitle;
        }

        protected override void Dispose(bool disposing){
            _stream=NewStream();
            base.Dispose(disposing);
        }

        public IObservable<ITraceEvent> EventTrace => _eventTraceSubject.ObserveOn(DefaultScheduler.Instance);

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message){
            base.TraceEvent(eventCache, source, eventType, id, message);
            
            var traceEvent = new TraceEventMessage{
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
            var regexObj = new Regex(@"(?<Location>[^.]*)\.(?<Method>[^(]*)\((?<Ln>[\d]*)\): (?<Action>[^(]*)\((?<Value>.*)\)",RegexOptions.Singleline);
            traceEvent.Location = regexObj.Match(traceEvent.Message).Groups["Location"].Value;
            traceEvent.Method = regexObj.Match(traceEvent.Message).Groups["Method"].Value;
            traceEvent.Value = regexObj.Match(traceEvent.Message).Groups["Value"].Value;
            traceEvent.Action = regexObj.Match(traceEvent.Message).Groups["Action"].Value;
            if (!string.IsNullOrEmpty(traceEvent.Action)){
                if (Enum.TryParse(traceEvent.Action, out RXAction rxAction)){
                    traceEvent.RXAction = rxAction;
                }
            }

            if (traceEvent.RXAction == RXAction.OnNext){
                traceEvent.ResultType = traceEvent.Method.Substring(traceEvent.Method.IndexOf(">", StringComparison.Ordinal) + 1);
                traceEvent.Method = traceEvent.Method.Substring(0, traceEvent.Method.IndexOf(" ", StringComparison.Ordinal));
            }
            traceEvent.Line = Convert.ToInt32(regexObj.Match(traceEvent.Message).Groups["Ln"].Value);
            traceEvent.LogicalOperationStack = string.Join(Environment.NewLine, eventCache.LogicalOperationStack.ToArray());
            
            _eventTraceSubject.OnNext(traceEvent);
        }

    }
}