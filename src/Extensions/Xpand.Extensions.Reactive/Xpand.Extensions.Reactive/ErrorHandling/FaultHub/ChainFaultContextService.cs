using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Reactive.ErrorHandling.FaultHub{
    public static class ChainFaultContextService {
        internal static readonly AsyncLocal<IImmutableStack<object>> ContextStack = new();

        public static IObservable<T> ChainFaultContext<T>(this IObservable<T> source, Func<IObservable<T>, IObservable<T>> retryStrategy,
            object[] context = null, [CallerMemberName] string caller = "")
            => (retryStrategy != null ? retryStrategy(source) : source).ChainFaultContext(context, caller);

        public static IObservable<T> ChainFaultContext<T>(this IObservable<T> source, 
            bool handleUpstreamRetries, object[] context = null, [CallerMemberName] string caller = "")
            => !handleUpstreamRetries ? source.ChainFaultContext(context, caller)
                : source.ChainFaultContext(errors => errors.Take(1).IgnoreElements(), context, caller);

        public static IObservable<T> ChainFaultContext<T>(this IObservable<T> source, [CallerMemberName] string caller = "")
            => source.ChainFaultContext([],caller);

        public static IObservable<T> PushStackFrame<T>(this IObservable<T> source, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0) {
            return Observable.Using(() => {
                var stackFromAbove = FaultHub.LogicalStackContext.Value;
                var myFrame = new LogicalStackFrame(memberName, filePath, lineNumber);
                var newStack = new[] { myFrame }.Concat(stackFromAbove ?? Enumerable.Empty<LogicalStackFrame>()).ToList();
                FaultHub.LogicalStackContext.Value = newStack;
                return Disposable.Create(FaultHub.LogicalStackContext,local => local.Value=stackFromAbove);
            }, _ => source);
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

        public static IObservable<T> ChainFaultContext<T>(this IObservable<T> source, object[] context,
            [CallerMemberName] string caller = "") {
            var trace = new StackTrace(true);
            var logicalStack = trace.LogicalStackFrames();
            return Observable.Using(
                () => {
                    var originalStack = FaultHub.LogicalStackContext.Value;
                    FaultHub.LogicalStackContext.Value = logicalStack;
                    return Disposable.Create(FaultHub.LogicalStackContext,local => local.Value=originalStack);
                },
                _ => {
                    var faultContext = context.NewFaultContext(caller);
                    var contextName = faultContext.Name;
                    return Observable.Defer(() => {
                        var parentHandlers = FaultHub.HandlersContext.Value;
                        return Observable.Using(
                                () => {
                                    var parentStack = ContextStack.Value ?? ImmutableStack<object>.Empty;
                                    ContextStack.Value = parentStack.Push(new object());
                                    FaultHub.HandlersContext.Value = new List<Func<Exception, FaultAction?>>();
                                    $"ChainFaultContext('{contextName}') - Using Setup (Scope Cleared)".LogAsyncLocalState();
                                    return Disposable.Create((parentStack, parentHandlers, contextName), static t => {
                                        ContextStack.Value = t.parentStack;
                                        FaultHub.HandlersContext.Value = t.parentHandlers;
                                        $"ChainFaultContext('{t.contextName}') - Using Dispose (Scope Restored)".LogAsyncLocalState();
                                    });
                                },
                                _ => {
                                    $"[HUB][WithFaultContext] Operator defined in '{contextName}'.".LogToConsole();
                                    return source;
                                }
                            )
                            .Catch<T, Exception>(ex => {
                                $"ChainFaultContext('{contextName}') - Catch Entry".LogAsyncLocalState();
                                var localHandlers = FaultHub.HandlersContext.Value;
                                try {
                                    var mergedHandlers = new List<Func<Exception, FaultAction?>>();
                                    if (parentHandlers != null) mergedHandlers.AddRange(parentHandlers);
                                    if (localHandlers != null) mergedHandlers.AddRange(localHandlers.Where(h => !mergedHandlers.Contains(h)));
                                    FaultHub.HandlersContext.Value = mergedHandlers;
                                    $"ChainFaultContext('{contextName}') - Catch Pre-Handle (Scope Merged)".LogAsyncLocalState();
                                    return ex.ProcessFault(
                                        faultContext,
                                        proceedAction: Observable.Throw<T>);
                                }
                                finally {
                                    FaultHub.HandlersContext.Value = localHandlers;
                                    $"ChainFaultContext('{contextName}') - Catch Finally (Scope Restored)".LogAsyncLocalState();
                                }
                            });
                    })
                    .SafeguardSubscription((e, s) => e.ExceptionToPublish(context.NewFaultContext(s)).Publish(), caller);
                });
            
        }


        public static void LogToConsole(this string message) {
            if (FaultHub.Logging) Console.WriteLine(message);
        }

        public static AmbientFaultContext NewFaultContext(this object[] context, [CallerMemberName]string memberName="",[CallerFilePath]string filePath="",[CallerLineNumber]int lineNumber=0) {
            $"[HUB-TRACE][NewFaultContext] Caller: '{memberName}', filePath: {filePath}, line: {lineNumber} Context: '{(context == null ? "null" : string.Join(", ", context))}'".LogToConsole();
            var finalContext = (context ?? []).Select(o => o?.ToString()).Where(s => !string.IsNullOrEmpty(s)).Distinct().WhereNotNullOrEmpty().ToList();
            if (!finalContext.Any()) {
                finalContext.Add(memberName);
            }
            return new AmbientFaultContext {
                LogicalStackTrace = FaultHub.LogicalStackContext.Value,
                CustomContext = finalContext.ToArray()
            };
        }
    }
}