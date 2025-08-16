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

        public static IObservable<T> ChainFaultContext<T>(this IObservable<T> source, Func<IObservable<T>, IObservable<T>> retryStrategy, object[] context = null)
            => source.ChainFaultContext(context ?? [], retryStrategy);

        public static IObservable<T> ChainFaultContext<T>(this IObservable<T> source, 
            bool handleUpstreamRetries, object[] context = null)
            => !handleUpstreamRetries ? source.ChainFaultContext(context)
                : source.ChainFaultContext(errors => errors.Take(1).IgnoreElements(), context);

        public static IObservable<T> ChainFaultContext<T>(this IObservable<T> source)
            => source.ChainFaultContext([]);


        public static IObservable<T> PushStackFrame<T>(this IObservable<T> source,object[] context,
            [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0 )
            => source.PushStackFrame( new LogicalStackFrame(memberName, filePath, lineNumber,context));

        public static IObservable<T> PushStackFrame<T>(this IObservable<T> source,
            [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
            => source.PushStackFrame( new LogicalStackFrame(memberName, filePath, lineNumber));
        
        private static IObservable<T> PushStackFrame<T>(this IObservable<T> source, LogicalStackFrame frame) 
            => Observable.Defer(() => {
                var stackFromAbove = FaultHub.LogicalStackContext.Value;
                var newStack = new[] { frame }.Concat(stackFromAbove ?? Enumerable.Empty<LogicalStackFrame>()).ToList();
                FaultHub.LogicalStackContext.Value = newStack;
                Log(() => $"[HUB-STACK][Push]   '{frame.MemberName}'. Stack depth: {newStack.Count}");
                return source;
            });

        private static void LogAsyncLocalState(this string step) {
            var handlerCount = FaultHub.HandlersContext.Value?.Count ?? -1;
            var nestingDepth = ContextStack.Value?.Count() ?? -1;
            Log(() => $"[HUB-DIAGNOSTIC][{step}] Handlers: {handlerCount}, Nesting: {nestingDepth}");
        }

        internal static IReadOnlyList<LogicalStackFrame> LogicalStackFrames(this StackTrace trace) 
            => trace.GetFrames()
                .Select(frame => {
                    var method = frame.GetMethod();
                    var type = method?.DeclaringType;
                    return type == null ? default : new LogicalStackFrame(method.Name, frame.GetFileName(), frame.GetFileLineNumber());
                })
                .WhereNotDefault()
                .ToList();


        public static IObservable<T> ChainFaultContext<T>(this IObservable<T> source, object[] context,
            Func<IObservable<T>, IObservable<T>> retryStrategy = null) {
            var contextName = context?.FirstOrDefault() ?? "UnnamedBoundary";
            return Observable.Defer(() => {
                var stackFromAbove = FaultHub.LogicalStackContext.Value;
                Log(() => $"[HUB-TRACE][ChainFaultContext Defer] Boundary created. Saving stack with depth {stackFromAbove?.Count ?? 0}.");

                var sourceWithAttemptClearing = Observable.Defer(() => {
                    FaultHub.LogicalStackContext.Value = null;
                    $"ChainFaultContext('{contextName}') - New attempt started".LogAsyncLocalState();
                    return source;
                });
                var effectiveSource = retryStrategy != null ? retryStrategy(sourceWithAttemptClearing) : sourceWithAttemptClearing;
                return effectiveSource
                    .Catch((Exception ex) => {
                        Log(() => "[HUB-TRACE][ChainFaultContext Catch] Boundary caught exception.");
                        var faultContext = context.NewFaultContext(FaultHub.LogicalStackContext.Value);
                        return ex.ProcessFault(faultContext, Observable.Throw<T>);
                    })
                    .Finally(() => {
                        FaultHub.LogicalStackContext.Value = stackFromAbove;
                        Log(() => $"[HUB-TRACE][ChainFaultContext Finally] Boundary disposed. Ambient stack restored to depth {stackFromAbove?.Count ?? 0}.");
                    });
            });
        }
        public static void Log(this Func<string> messageSelector) {
            if (FaultHub.Logging) Console.WriteLine(messageSelector());
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
            Log(() => $"[HUB-TRACE][NewFaultContext] Caller: '{memberName}', filePath: {filePath}, line: {lineNumber} Context: '{(context == null ? "null" : string.Join(", ", context))}'");
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