using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Xpand.Extensions.LinqExtensions;

namespace Xpand.Extensions.Reactive.ErrorHandling.FaultHub{
    public static class ChainFaultContextService {
        internal static readonly AsyncLocal<IImmutableStack<object>> ContextStack = new();

        public static IObservable<T> ChainFaultContext<T>(this IObservable<T> source, Func<IObservable<T>, IObservable<T>> retryStrategy,
            object[] context = null, [CallerMemberName] string caller = "")
            => source.ChainFaultContext(context ?? [], retryStrategy, caller);

        public static IObservable<T> ChainFaultContext<T>(this IObservable<T> source, 
            bool handleUpstreamRetries, object[] context = null, [CallerMemberName] string caller = "")
            => !handleUpstreamRetries ? source.ChainFaultContext(context,null, caller)
                : source.ChainFaultContext(errors => errors.Take(1).IgnoreElements(), context, caller);

        public static IObservable<T> ChainFaultContext<T>(this IObservable<T> source, [CallerMemberName] string caller = "")
            => source.ChainFaultContext([],null,caller);


        public static IObservable<T> PushStackFrame<T>(this IObservable<T> source,object[] context,
            [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0 )
            => source.PushStackFrame( new LogicalStackFrame(memberName, filePath, lineNumber,context));

        public static IObservable<T> PushStackFrame<T>(this IObservable<T> source,
            [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
            => source.PushStackFrame( new LogicalStackFrame(memberName, filePath, lineNumber));

// MODIFICATION: The operator is radically simplified. Its ONLY responsibility is to push a frame.
// It no longer has any pop/cleanup logic. It relies entirely on a boundary operator like ChainFaultContext to manage the stack's lifecycle.
        private static IObservable<T> PushStackFrame<T>(this IObservable<T> source, LogicalStackFrame frame) {
            return Observable.Defer(() => {
                var stackFromAbove = FaultHub.LogicalStackContext.Value;
                var newStack = new[] { frame }.Concat(stackFromAbove ?? Enumerable.Empty<LogicalStackFrame>()).ToList();
                FaultHub.LogicalStackContext.Value = newStack;
                $"[HUB-STACK][Push]   '{frame.MemberName}'. Stack depth: {newStack.Count}".LogToConsole();
                return source;
            });
        }
        
        private static void LogAsyncLocalState(this string step) {
            var handlerCount = FaultHub.HandlersContext.Value?.Count ?? -1;
            var nestingDepth = ContextStack.Value?.Count() ?? -1;
            $"[HUB-DIAGNOSTIC][{step}] Handlers: {handlerCount}, Nesting: {nestingDepth}".LogToConsole();
        }

        internal static IReadOnlyList<LogicalStackFrame> LogicalStackFrames(this StackTrace trace) {
            return trace.GetFrames()
                .Select(frame => {
                    var method = frame.GetMethod();
                    var type = method?.DeclaringType;
                    if (type == null) return default;
                    var fileName = frame.GetFileName();
                    var lineNumber = frame.GetFileLineNumber();
                    return new LogicalStackFrame(method.Name, fileName, lineNumber);
                })
                .WhereNotDefault()
                .ToList();
        }


// MODIFICATION: This operator now takes full responsibility for the stack lifecycle.
// It saves the stack state when it starts and restores it in a Finally block when it ends.
// Crucially, it uses an inner Defer to RESET the stack on EACH retry attempt, preventing pollution.
// MODIFICATION: The call to ex.ProcessFault has been restored. This is critical for allowing
// high-priority handlers like CompleteOnFault and RethrowOnFault to function correctly
// within the resilience boundary.
        public static IObservable<T> ChainFaultContext<T>(this IObservable<T> source, object[] context,
            Func<IObservable<T>, IObservable<T>> retryStrategy = null, [CallerMemberName] string caller = "") {
    
            return Observable.Defer(() => {
                var stackFromAbove = FaultHub.LogicalStackContext.Value;
                $"[HUB-TRACE][ChainFaultContext Defer] Boundary created. Saving stack with depth {stackFromAbove?.Count ?? 0}.".LogToConsole();

                var sourceWithAttemptClearing = Observable.Defer(() => {
                    // On each subscription/retry, reset the stack to a clean slate (null).
                    FaultHub.LogicalStackContext.Value = null;
                    $"[HUB-TRACE][ChainFaultContext Attempt] New attempt started. Stack cleared.".LogToConsole();
                    return source;
                });

                var effectiveSource = retryStrategy != null ? retryStrategy(sourceWithAttemptClearing) : sourceWithAttemptClearing;

                return effectiveSource
                    .Catch((Exception ex) => {
                        $"[HUB-TRACE][ChainFaultContext Catch] Boundary caught exception.".LogToConsole();
                        var faultContext = context.NewFaultContext(FaultHub.LogicalStackContext.Value, caller);
                        return ex.ProcessFault(faultContext, enrichedException => Observable.Throw<T>(enrichedException));
                    })
                    .Finally(() => {
                        FaultHub.LogicalStackContext.Value = stackFromAbove;
                        $"[HUB-TRACE][ChainFaultContext Finally] Boundary disposed. Ambient stack restored to depth {stackFromAbove?.Count ?? 0}.".LogToConsole();
                    });
            });
        }
        public static void LogToConsole(this string message) {
            if (FaultHub.Logging) Console.WriteLine(message);
        }
        public static IObservable<T> FlowFaultContext<T>(this IObservable<T> source) {
            return Observable.Create<T>(observer => {
                var capturedContext = ExecutionContext.Capture();
                return capturedContext == null
                    ? source.Subscribe(observer)
                    : source.Subscribe(
                        onNext: value => ExecutionContext.Run(capturedContext, _ => observer.OnNext(value), null),
                        onError: error => ExecutionContext.Run(capturedContext, _ => observer.OnError(error), null),
                        onCompleted: () => ExecutionContext.Run(capturedContext, _ => observer.OnCompleted(), null)
                    );
            });
        }
        
        public static AmbientFaultContext NewFaultContext(this object[] context, IReadOnlyList<LogicalStackFrame> logicalStack, [CallerMemberName]string memberName="",[CallerFilePath]string filePath="",[CallerLineNumber]int lineNumber=0) {
            $"[HUB-TRACE][NewFaultContext] Caller: '{memberName}', filePath: {filePath}, line: {lineNumber} Context: '{(context == null ? "null" : string.Join(", ", context))}'".LogToConsole();
            var finalContext = (context ?? []).Select(o => o).WhereNotDefault().Distinct().ToList();
            if (!finalContext.Any()) {
                finalContext.Add(memberName);
            }
            return new AmbientFaultContext {
                LogicalStackTrace = logicalStack,
                CustomContext = finalContext.ToArray()
            };
        }
        
    }
}