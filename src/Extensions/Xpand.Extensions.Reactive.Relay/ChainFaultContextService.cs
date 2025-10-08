using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Xpand.Extensions.ExceptionExtensions;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.StringExtensions;

namespace Xpand.Extensions.Reactive.Relay{
    public static class ChainFaultContextService {
        internal static readonly AsyncLocal<IImmutableStack<object>> ContextStack=NewContext<IImmutableStack<object>>();

        public static IObservable<T> ChainFaultContext<T>(this IObservable<T> source, Func<IObservable<T>, IObservable<T>> retryStrategy, object[] context = null,
            [CallerMemberName]string memberName="",[CallerFilePath]string filePath="",[CallerLineNumber]int lineNumber=0)
            => source.ChainFaultContext(context ?? [], retryStrategy,memberName, filePath, lineNumber);
        public static IObservable<T> ChainFaultContext<T>(this IObservable<T> source, Func<IObservable<T>, IObservable<T>> retryStrategy,(string memberName,string filePath,int lineNumber) caller, object[] context = null)
            => source.ChainFaultContext(retryStrategy,context,caller.memberName,caller.filePath,caller.lineNumber);

        public static IObservable<T> ChainFaultContext<T>(this IObservable<T> source, 
            bool handleUpstreamRetries, object[] context = null,[CallerMemberName]string memberName="",[CallerFilePath]string filePath="",[CallerLineNumber]int lineNumber=0)
            => !handleUpstreamRetries ? source.ChainFaultContext(context,null,memberName, filePath, lineNumber)
                : source.ChainFaultContext(errors => errors.Take(1).IgnoreElements(), context, memberName, filePath, lineNumber);

        public static IObservable<T> ChainFaultContext<T>(this IObservable<T> source,[CallerMemberName]string memberName="",[CallerFilePath]string filePath="",[CallerLineNumber]int lineNumber=0)
            => source.ChainFaultContext([],null, memberName, filePath, lineNumber);
        
        public static IObservable<T> PushStackFrame<T>(this IObservable<T> source,object[] context,
            [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0 )
            => !FaultHub.Enabled ? source : source.PushStackFrame(new LogicalStackFrame(memberName, filePath, lineNumber, context));

        public static IObservable<T> PushStackFrame<T>(this IObservable<T> source,
            [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
            => !FaultHub.Enabled ? source : source.PushStackFrame(new LogicalStackFrame(memberName, filePath, lineNumber));
        public static IObservable<T> PushStackFrame<T>(this IObservable<T> source, (string memberName,string filePath,int lineNumber) caller)
            => source.PushStackFrame(caller.memberName, caller.filePath, caller.lineNumber);
        
        internal static IImmutableStack<LogicalStackFrame> LogicalStackFrames(this StackTrace stackTrace) 
            => stackTrace.GetFrames().Select(frame => {
                var method = frame.GetMethod();
                return new LogicalStackFrame(method?.Name, frame.GetFileName(), frame.GetFileLineNumber(), method?.DeclaringType?.Namespace);
            }).ToList().ToImmutableStack();
        
        public static IObservable<T> ChainFaultContext<T>(this IObservable<T> source, object[] context, Func<IObservable<T>, IObservable<T>> retryStrategy = null,
            [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0,IReadOnlyList<string> tags = null)
            => Observable.Defer(() => {
                LogFast($"Entering Defer for boundary '{memberName}'.");
                var originalStack = FaultHub.LogicalStackContext.Value;
                FaultHub.LogicalStackContext.Value = ImmutableStack<LogicalStackFrame>.Empty;
                LogFast($"Cleared logical stack (was {originalStack?.Count() ?? 0} frames).");
                var resilientSource = retryStrategy != null ? retryStrategy(source) : source;
                return resilientSource.Catch((Exception e) => e.ProcessFaultContext<T>(context, memberName, filePath, lineNumber, tags))
                    .Finally(() => {
                        LogFast($"Finally block entered for boundary '{memberName}'.");
                        FaultHub.LogicalStackContext.Value = originalStack;
                        LogFast($"Restored logical stack to {originalStack?.Count() ?? 0} frames.");
                    })
                    .FaultHubFlowContext();
            });



        private static IObservable<T> ProcessFaultContext<T>(this Exception e,object[] context, string memberName, string filePath, int lineNumber, IReadOnlyList<string> tags){
            var fullStack = FaultHub.LastKnownStack.Value ?? ImmutableStack<LogicalStackFrame>.Empty;
            FaultHub.LastKnownStack.Value = null;
            
            var stackTraceForLog = string.Join(" -> ", fullStack.Select(f => f.MemberName));
            LogFast($"Reassembled stack. Total: {fullStack.Count()}. Stack: [{stackTraceForLog}]");
    
            if (fullStack.All(f => f.MemberName != memberName)) {
                fullStack = fullStack.Push( new LogicalStackFrame(memberName.Remove(" "),filePath, lineNumber, context));
            }
            var faultContext = fullStack.NewFaultContext(context,tags,memberName, filePath);
            return e.ProcessFault(faultContext, Observable.Throw<T>);        }

        private static IObservable<T> PushStackFrame<T>(this IObservable<T> source, LogicalStackFrame frame) 
            => Observable.Create<T>(observer => {
                if (FaultHub.LogicalStackContext.Value?.FirstOrDefault().Equals(frame) ?? false) {
                    LogFast($"Skipping duplicate frame: {frame.MemberName}");
                    return source.Subscribe(observer);
                }

                return source.Subscribe(
                    value => {
                        using (new StackManager(frame)) {
                            observer.OnNext(value);
                        }
                    },
                    ex => {
                        using (new StackManager(frame)) {
                            
                            var currentStack = FaultHub.LogicalStackContext.Value;
                            FaultHub.LastKnownStack.Value = currentStack;

                            observer.OnError(ex);                        }                    },
                    () => {
                        using (new StackManager(frame)) {
                            observer.OnCompleted();
                        }
                    }
                );
            });
        
        private readonly struct StackManager : IDisposable {
            private readonly IImmutableStack<LogicalStackFrame> _originalStack;

            public StackManager(LogicalStackFrame frame) {
                _originalStack = FaultHub.LogicalStackContext.Value;
                var newStack = (_originalStack ?? ImmutableStack<LogicalStackFrame>.Empty).Push(frame);
                FaultHub.LogicalStackContext.Value = newStack;
            }

            public void Dispose() {
                FaultHub.LogicalStackContext.Value = _originalStack;
            }
        }
        
        public static AmbientFaultContext NewFaultContext(this IImmutableStack<LogicalStackFrame> logicalStack,object[] context,IReadOnlyList<string> tags = null, [CallerMemberName]string memberName="",[CallerFilePath]string filePath="") {
            LogFast($"Caller: '{memberName}', filePath: {filePath} Context: '{(context == null ? "null" : string.Join(", ", context))}'");
            return new AmbientFaultContext { LogicalStackTrace = logicalStack, BoundaryName = memberName, UserContext = context ?? [],Tags = tags??[]};
        }
    }
}