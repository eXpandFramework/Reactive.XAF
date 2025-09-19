using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
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
        
        private static void LogAsyncLocalState(this Func<string> step) {
            var handlerCount = FaultHub.HandlersContext.Value?.Count ?? -1;
            var nestingDepth = ContextStack.Value?.Count() ?? -1;
            LogFast($"[HUB-DIAGNOSTIC][{step}] Handlers: {handlerCount}, Nesting: {nestingDepth}");
        }

        internal static IReadOnlyList<LogicalStackFrame> LogicalStackFrames(this StackTrace stackTrace) 
            => stackTrace.GetFrames().Select(frame => {
                var method = frame.GetMethod();
                return new LogicalStackFrame(method?.Name, frame.GetFileName(), frame.GetFileLineNumber(), method?.DeclaringType?.Namespace);
            }).ToList();
        
        public static IObservable<T> ChainFaultContext<T>(this IObservable<T> source, object[] context, Func<IObservable<T>, IObservable<T>> retryStrategy = null,
            [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0,IReadOnlyList<string> tags = null)
            => Observable.Defer(() => {
                LogFast($"[ChainCtx] Entering Defer for boundary '{memberName}'.");
                var snapshot = new FaultSnapshot();
                var originalSnapshot = FaultHub.CurrentFaultSnapshot.Value;
                var originalStack = snapshot.CaptureOriginalStack(memberName);
                LogFast($"[ChainCtx] Cleared logical stack (was {originalStack?.Count ?? 0} frames).");
                var resilientSource = retryStrategy != null ? retryStrategy(source) : source;
                return resilientSource.Catch((Exception e) => e.ProcessFaultContext<T>(context, memberName, filePath, lineNumber, tags,  snapshot, originalStack))
                    .Finally(() => originalSnapshot.RestoreFault(memberName, originalStack))
                    .FaultHubFlowContext();
            });

        private static IReadOnlyList<LogicalStackFrame> CaptureOriginalStack(this FaultSnapshot snapshot,string memberName){
            FaultHub.CurrentFaultSnapshot.Value = snapshot;
            LogFast($"[CTX-TRACE][ChainCtx] Boundary '{memberName}'. Snapshot in AsyncLocal is null? {FaultHub.CurrentFaultSnapshot.Value == null}.");
            var originalStack = FaultHub.LogicalStackContext.Value;
            FaultHub.LogicalStackContext.Value = null;
            return originalStack;
        }

        private static void RestoreFault(this FaultSnapshot originalSnapshot,string memberName, IReadOnlyList<LogicalStackFrame> originalStack){
            LogFast($"[ChainCtx] Finally block entered for boundary '{memberName}'.");
            FaultHub.CurrentFaultSnapshot.Value = originalSnapshot;
            FaultHub.LogicalStackContext.Value = originalStack;
            LogFast($"[ChainCtx] Restored logical stack to {originalStack?.Count ?? 0} frames.");
        }

        private static IObservable<T> ProcessFaultContext<T>(this Exception e,object[] context, string memberName, string filePath, int lineNumber,
            IReadOnlyList<string> tags, FaultSnapshot snapshot, IReadOnlyList<LogicalStackFrame> originalStack){
            LogFast($"[ChainCtx] Catch block entered for boundary '{memberName}'. Exception: {e.GetType().Name}");
            LogFast($"[ChainCtx] Reading stack from snapshot. Found {snapshot.CapturedStack?.Count ?? 0} frames.");
            var fullStack = (snapshot.CapturedStack ?? Enumerable.Empty<LogicalStackFrame>()).ToList();
            var stackTraceForLog = string.Join(" -> ", fullStack.Select(f => f.MemberName));
            LogFast($"[CTX-TRACE][ChainCtx-Catch] Reassembled stack. Inner: {snapshot.CapturedStack?.Count ?? 0}, Parent: {originalStack?.Count ?? 0}, Total: {fullStack.Count}. Stack: [{stackTraceForLog}]");
            if (fullStack.All(f => f.MemberName != memberName)) {
                fullStack.Add( new LogicalStackFrame(memberName.Remove(" "),filePath, lineNumber, context));
            }
            var faultContext = fullStack.NewFaultContext(context,tags,memberName, filePath, lineNumber);
            return e.ProcessFault(faultContext, Observable.Throw<T>);
        }

        private static IObservable<T> PushStackFrame<T>(this IObservable<T> source, LogicalStackFrame frame) 
            => Observable.Defer(() => {
                LogAsyncLocalState(() => $"Before PushStackFrame '{frame.MemberName}'");
                var originalStack = FaultHub.LogicalStackContext.Value;
                if (originalStack?.FirstOrDefault().Equals(frame) ?? false) {
                    LogFast($"[PushStackFrame] Skipping duplicate frame: {frame.MemberName}");
                    return source.FaultHubFlowContext();
                }
                frame.UpdateStackForFrame( originalStack);
                return source.Materialize()
                    .Do(notification => {
                        if (notification.Kind != NotificationKind.OnError) return;
                        notification.SaveCurrentStackToSnapshot( frame);
                    })
                    .Dematerialize()
                    .Finally(() => frame.RestoreOriginalStack(originalStack))
                    .FaultHubFlowContext();
            });

        private static void RestoreOriginalStack(this LogicalStackFrame frame, IReadOnlyList<LogicalStackFrame> originalStack){
            LogFast($"[PushStackFrame] Finally: Restoring original stack for '{frame.MemberName}' to {originalStack?.Count ?? 0} frames.");
            FaultHub.LogicalStackContext.Value = originalStack;
            LogAsyncLocalState(() => $"After PushStackFrame '{frame.MemberName}'");
        }

        private static void SaveCurrentStackToSnapshot<T>(this  Notification<T> notification,LogicalStackFrame frame){
            var currentStack = FaultHub.LogicalStackContext.Value;
            if (currentStack != null) {
                notification.Exception!.Data[FaultHub.CapturedStackKey] = currentStack;
                var currentStackCount = currentStack.Count;
                LogFast($"[PushStackFrame] Attached stack with {currentStackCount} frames to Exception.Data.");
            }
            LogFast($"[CTX-TRACE][PushStackFrame-OnError] Frame '{frame.MemberName}'. Stack Frames: {FaultHub.LogicalStackContext.Value?.Count ?? 0}. Is Snapshot null? {FaultHub.CurrentFaultSnapshot.Value == null}.");
            LogFast($"[PushStackFrame] OnError detected in '{frame.MemberName}'.");
            var snapshot = FaultHub.CurrentFaultSnapshot.Value;
            if (snapshot != null) {
                currentStack = FaultHub.LogicalStackContext.Value;
                LogFast($"[PushStackFrame] Current snapshot has {snapshot.CapturedStack?.Count ?? 0} frames. Current logical stack has {currentStack?.Count ?? 0} frames.");
                if ((currentStack?.Count ?? 0) > (snapshot.CapturedStack?.Count ?? 0)) {
                    LogFast($"[PushStackFrame] Saving stack with {currentStack?.Count} frames to snapshot.");
                    snapshot.CapturedStack = currentStack;
                }
                else {
                    LogFast($"[PushStackFrame] Stack is not longer than what's in snapshot. Not saving.");
                }
            }
            else {
                LogFast($"[PushStackFrame] Snapshot was null. Cannot save stack.");
            }
        }

        private static void UpdateStackForFrame(this LogicalStackFrame frame, IReadOnlyList<LogicalStackFrame> originalStack){
            var newStack = new[] { frame }.Concat(originalStack ?? Enumerable.Empty<LogicalStackFrame>()).ToList();
            FaultHub.LogicalStackContext.Value = newStack;
            LogFast($"[PushStackFrame] Pushed '{frame.MemberName}'. New stack depth: {newStack.Count}.");
            LogFast($"[SUB-TRACE][PushStackFrame-Defer] Executed for frame '{frame.MemberName}'. New Stack Count: {newStack.Count}.");
            LogFast($"[PushStackFrame] Pushed '{frame.MemberName}'. New stack depth: {newStack.Count}.");
        }

        public static AmbientFaultContext NewFaultContext(this  IReadOnlyList<LogicalStackFrame> logicalStack,object[] context,IReadOnlyList<string> tags = null, [CallerMemberName]string memberName="",[CallerFilePath]string filePath="",[CallerLineNumber]int lineNumber=0) {
            LogFast($"[HUB-TRACE][NewFaultContext] Caller: '{memberName}', filePath: {filePath}, line: {lineNumber} Context: '{(context == null ? "null" : string.Join(", ", context))}'");
            return new AmbientFaultContext { LogicalStackTrace = logicalStack, BoundaryName = memberName, UserContext = context ?? [],Tags = tags??[]};
        }
    }
}