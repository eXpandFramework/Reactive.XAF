using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Reactive.Relay{

    internal class ExceptionWithLogicalContext(Exception originalException, ImmutableList<LogicalStackFrame> contextPath) : Exception(originalException.Message, originalException) {
        public ImmutableList<LogicalStackFrame> ContextPath => contextPath;
        public Exception OriginalException => InnerException;
        
    };

    internal class PushStackFrameObserver<T>(IObserver<T> downstream, LogicalStackFrame frame, bool preserveContext) : IObserver<T> {
        public void OnNext(T value) {
            var originalContext = FaultHub.LogicalStackContext.Value;
            try {
                var currentStack = originalContext ?? ImmutableList<LogicalStackFrame>.Empty;
                FaultHub.LogicalStackContext.Value = currentStack.Insert(0, frame);
                downstream.OnNext(value);
            }
            finally {
                if (!preserveContext && !FaultHub.PreserveLogicalStack.Value) {
                    FaultHub.LogicalStackContext.Value = originalContext;
                }
            }
        }
        
        public void OnError(Exception error) {
            LogFast($"PushStackFrameObserver received exception of type: {error.GetType().FullName}");
            if (error.GetType() ==typeof(FaultHubException) ) {
                downstream.OnError(error);
                return;
            }
            var currentStack = FaultHub.LogicalStackContext.Value ?? ImmutableList<LogicalStackFrame>.Empty;
            var stackWithFrame = currentStack.Insert(0, frame);

            if (error is ExceptionWithLogicalContext contextException) {
                var newPath = contextException.ContextPath.Add(frame);
                downstream.OnError(new ExceptionWithLogicalContext(contextException.OriginalException, newPath));
            }
            else {
                downstream.OnError(new ExceptionWithLogicalContext(error, stackWithFrame));
            }
        }
 


        public void OnCompleted() => downstream.OnCompleted();
    }
public static class ChainFaultContextService {
        internal static IReadOnlyList<LogicalStackFrame> LogicalStackFrames(this System.Diagnostics.StackTrace stackTrace) 
            => stackTrace.GetFrames().Select(frame => {
                var method = frame.GetMethod();
                return new LogicalStackFrame(method?.Name, frame.GetFileName(), frame.GetFileLineNumber(), method?.DeclaringType?.Namespace);
            }).ToList();

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
        
        
        public static IObservable<T> ChainFaultContext<T>(this IObservable<T> source, object[] context, Func<IObservable<T>, IObservable<T>> retryStrategy = null,
            [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0,IReadOnlyList<string> tags = null)
            => Observable.Defer(() => {
                var resilientSource = retryStrategy != null ? retryStrategy(source) : source;
                return resilientSource.Catch((Exception e) => {
                    LogFast($"Catch block entered for boundary '{memberName}'. Exception: {e.GetType().Name}");

                    IReadOnlyList<LogicalStackFrame> logicalStack;
                    Exception originalException;

                    if (e is ExceptionWithLogicalContext contextException) {
                        logicalStack = contextException.ContextPath;
                        originalException = contextException.OriginalException;
                        LogFast($"Exception has logical context with {logicalStack.Count} frames.");
                    }
                    else {
                        logicalStack = FaultHub.LogicalStackContext.Value ?? (IReadOnlyList<LogicalStackFrame>)ImmutableList<LogicalStackFrame>.Empty;
                        originalException = e;
                        LogFast($"Exception has no logical context. Using ambient stack with {logicalStack.Count} frames.");
                    }
                
                    var fullStack = logicalStack.ToList();
                    var existingFrameIndex = fullStack.FindIndex(f => f.MemberName == memberName);
                    if (existingFrameIndex > -1) {
                        var existingFrame = fullStack[existingFrameIndex];
                        var newContext = (existingFrame.Context ?? []).Concat(context ?? []).Distinct().ToArray();
                        fullStack[existingFrameIndex] = new LogicalStackFrame(memberName, filePath, lineNumber, newContext);
                    }
                    else {
                        fullStack.Add(new LogicalStackFrame(memberName, filePath, lineNumber, context));
                    }
                
                    var faultContext = fullStack.NewFaultContext(context, tags, memberName, filePath);
                    return originalException.ProcessFault(faultContext, Observable.Throw<T>);
                });
            }).UseContext(true, FaultHub.IsChainingActive.Wrap());
        
        public static IObservable<T> PushStackFrame<T>(this IObservable<T> source, LogicalStackFrame frame, bool preserveContext = false)  
            => Observable.Defer(() => {
                var originalStack = FaultHub.LogicalStackContext.Value;
                if (originalStack?.FirstOrDefault().Equals(frame) ?? false) {
                    LogFast($"Skipping duplicate frame: {frame.MemberName}");
                    return source;
                }
                LogFast($"Pushing frame '{frame.MemberName}'.");
                return Observable.Create<T>(observer => source.Subscribe(new PushStackFrameObserver<T>(observer, frame, preserveContext)));
            });

        public static AmbientFaultContext NewFaultContext(this  IReadOnlyList<LogicalStackFrame> logicalStack,object[] context,IReadOnlyList<string> tags = null, [CallerMemberName]string memberName="",[CallerFilePath]string filePath="") {
            LogFast($"Caller: '{memberName}', filePath: {filePath} Context: '{(context == null ? "null" : string.Join(", ", context))}'");
            return new AmbientFaultContext { LogicalStackTrace = logicalStack, BoundaryName = memberName, UserContext = context ?? [],Tags = tags??[]};
        }
    }
}