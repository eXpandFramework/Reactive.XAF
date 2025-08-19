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
                    var method = frame.GetMethod();
                    var type = method?.DeclaringType;
                    return type == null ? default : new LogicalStackFrame(method.Name, frame.GetFileName(), frame.GetFileLineNumber());
                })
                .WhereNotDefault()
                .ToList();


        public static IObservable<T> ChainFaultContext<T>(this IObservable<T> source, object[] context, Func<IObservable<T>, IObservable<T>> retryStrategy = null,
            [CallerMemberName]string memberName="",[CallerFilePath]string filePath="",[CallerLineNumber]int lineNumber=0) {
            if (!FaultHub.Enabled)return source;
            
            var resilientSource = retryStrategy != null ? retryStrategy(source) : source;

            return resilientSource.Catch((Exception ex) => {
                var faultContext = context.NewFaultContext(FaultHub.LogicalStackContext.Value, memberName, filePath, lineNumber);
                return ex.ProcessFault(faultContext, Observable.Throw<T>);
            });
        }

        private static IObservable<T> PushStackFrame<T>(this IObservable<T> source, LogicalStackFrame frame) 
            => Observable.Defer(() => {
                var stackFromAbove = FaultHub.LogicalStackContext.Value;
                var topFrame = stackFromAbove?.FirstOrDefault();
                if (topFrame?.MemberName == frame.MemberName) {
                    return source;
                }
                var newStack = new[] { frame }.Concat(stackFromAbove ?? Enumerable.Empty<LogicalStackFrame>()).ToList();
                FaultHub.LogicalStackContext.Value = newStack;
                return source;
            });

        public static void Log(this Func<string> messageSelector) {
            if (FaultHub.Logging) Console.WriteLine(messageSelector());
        }
        
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