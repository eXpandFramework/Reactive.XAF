using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Reactive.Utility;
using static Xpand.Extensions.Reactive.ErrorHandling.FaultHub.FaultHubLogger;
namespace Xpand.Extensions.Reactive.ErrorHandling.FaultHub{
    public static class ChainFaultContextService {
        internal static readonly AsyncLocal<IImmutableStack<object>> ContextStack = new();
        internal static readonly IAsyncLocal[] All = [ ContextStack.Wrap()];

        public static IObservable<T> ChainFaultContext<T>(this IObservable<T> source, Func<IObservable<T>, IObservable<T>> retryStrategy, object[] context = null,
            [CallerMemberName]string memberName="",[CallerFilePath]string filePath="",[CallerLineNumber]int lineNumber=0)
            => source.ChainFaultContext(context ?? [], retryStrategy,memberName, filePath, lineNumber);

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


        private static void LogAsyncLocalState(this Func<string> step) {
            var handlerCount = FaultHub.HandlersContext.Value?.Count ?? -1;
            var nestingDepth = ContextStack.Value?.Count() ?? -1;
            Log(() => $"[HUB-DIAGNOSTIC][{step}] Handlers: {handlerCount}, Nesting: {nestingDepth}");
        }

        internal static IReadOnlyList<LogicalStackFrame> LogicalStackFrames(this StackTrace trace) 
            => trace.GetFrames()
                .Select(frame => {
                    var methodBase = frame.GetMethod();
                    return methodBase?.DeclaringType == null ? default
                        : new LogicalStackFrame(methodBase.Name, frame.GetFileName(), frame.GetFileLineNumber());
                })
                .WhereNotDefault()
                .ToList();


        public static IObservable<T> ChainFaultContext<T>(this IObservable<T> source, object[] context,
            Func<IObservable<T>, IObservable<T>> retryStrategy = null,
            [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0) {
            if (!FaultHub.Enabled) return source;

            return Observable.Defer(() => {
                Log(() => $"[ChainCtx] Entering Defer for boundary '{memberName}'.");
                
                
                var snapshot = new FaultSnapshot();
                var originalSnapshot = FaultHub.CurrentFaultSnapshot.Value;
                FaultHub.CurrentFaultSnapshot.Value = snapshot;
                
                
                var originalStack = FaultHub.LogicalStackContext.Value;
                FaultHub.LogicalStackContext.Value = null;
                Log(() => $"[ChainCtx] Cleared logical stack (was {originalStack?.Count ?? 0} frames).");

                var resilientSource = retryStrategy != null ? retryStrategy(source) : source;

                return resilientSource
                    .Catch((Exception ex) => {
                        Log(() => $"[ChainCtx] Catch block entered for boundary '{memberName}'. Exception: {ex.GetType().Name}");
                        Log(() => $"[ChainCtx] Reading stack from snapshot. Found {snapshot.CapturedStack?.Count ?? 0} frames.");
                        var faultContext =
                            context.NewFaultContext(snapshot.CapturedStack, memberName, filePath, lineNumber);
                        return ex.ProcessFault(faultContext, Observable.Throw<T>);
                    })
                    .Finally(() => {
                        
                        Log(() => $"[ChainCtx] Finally block entered for boundary '{memberName}'.");
                        FaultHub.CurrentFaultSnapshot.Value = originalSnapshot;
                        FaultHub.LogicalStackContext.Value = originalStack;
                        Log(() => $"[ChainCtx] Restored logical stack to {originalStack?.Count ?? 0} frames.");
                    });
            });
        }        

        private static IObservable<T> PushStackFrame<T>(this IObservable<T> source, LogicalStackFrame frame)
            => Observable.Defer(() => {
                LogAsyncLocalState(() => $"Before PushStackFrame '{frame.MemberName}'");
                var originalStack = FaultHub.LogicalStackContext.Value;

                if (originalStack?.FirstOrDefault().MemberName == frame.MemberName) {
                    Log(() => $"[PushStackFrame] Skipping duplicate frame: {frame.MemberName}");
                    return source;
                }

                var newStack = new[] { frame }.Concat(originalStack ?? Enumerable.Empty<LogicalStackFrame>()).ToList();
                FaultHub.LogicalStackContext.Value = newStack;
                Log(() => $"[PushStackFrame] Pushed '{frame.MemberName}'. New stack depth: {newStack.Count}.");

                return source.Materialize()
                    .Do(notification => {
                        if (notification.Kind != NotificationKind.OnError) return;
                        Log(() => $"[PushStackFrame] OnError detected in '{frame.MemberName}'.");
                        var snapshot = FaultHub.CurrentFaultSnapshot.Value;
                        if (snapshot != null) {
                            var currentStack = FaultHub.LogicalStackContext.Value;
                            Log(() => $"[PushStackFrame] Current snapshot has {snapshot.CapturedStack?.Count ?? 0} frames. Current logical stack has {currentStack?.Count ?? 0} frames.");
                            if ((currentStack?.Count ?? 0) > (snapshot.CapturedStack?.Count ?? 0)) {
                                Log(() => $"[PushStackFrame] Saving stack with {currentStack?.Count} frames to snapshot.");
                                snapshot.CapturedStack = currentStack;
                            }
                            else {
                                Log(() => "[PushStackFrame] Stack is not longer than what's in snapshot. Not saving.");
                            }
                        }
                        else {
                            Log(() => "[PushStackFrame] Snapshot was null. Cannot save stack.");
                        }
                    })
                    .Dematerialize()
                    .Finally(() => {
                        Log(() => $"[PushStackFrame] Finally: Restoring original stack for '{frame.MemberName}' to {originalStack?.Count ?? 0} frames.");
                        FaultHub.LogicalStackContext.Value = originalStack;
                        LogAsyncLocalState(() => $"After PushStackFrame '{frame.MemberName}'");
                    });
            });
        
        
        public static AmbientFaultContext NewFaultContext(this object[] context, IReadOnlyList<LogicalStackFrame> logicalStack, [CallerMemberName]string memberName="",[CallerFilePath]string filePath="",[CallerLineNumber]int lineNumber=0) {
            Log(() => $"[HUB-TRACE][NewFaultContext] Caller: '{memberName}', filePath: {filePath}, line: {lineNumber} Context: '{(context == null ? "null" : string.Join(", ", context))}'");
            var finalContext = (context ?? []).Select(o => o).WhereNotDefault().Prepend(memberName).Distinct().ToList();
            return new AmbientFaultContext {
                LogicalStackTrace = logicalStack,
                CustomContext = finalContext.ToArray()
            };
        }

    }
    
}