using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Extensions.Caching.Memory;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Reactive.ErrorHandling.FaultHub{
    public static class ChainFaultContextService {
        
        internal static readonly AsyncLocal<Stack<object>> ContextStack = new();
        internal static readonly AsyncLocal<StackTrace> InvocationTrace = new();
        
        private static readonly MemoryCache StackTraceStringCache = new(new MemoryCacheOptions());

        
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
                var newStack = new[] { myFrame }.Concat(stackFromAbove ?? Enumerable.Empty<LogicalStackFrame>())
                    .ToList();
                FaultHub.LogicalStackContext.Value = newStack;
                return Disposable.Create(() => FaultHub.LogicalStackContext.Value = stackFromAbove);
            }, _ => source);
        }

        private static void LogAsyncLocalState(this string step) {
            var handlerCount = FaultHub.HandlersContext.Value?.Count ?? -1;
            var nestingDepth = ContextStack.Value?.Count ?? -1;
            $"[HUB-DIAGNOSTIC][{step}] Handlers: {handlerCount}, Nesting: {nestingDepth}".LogToConsole();
        }
        
        // This helper replaces GetOrAddCachedStackTraceString.
        // It converts a physical StackTrace into our logical structure.
        internal static IReadOnlyList<LogicalStackFrame> GetLogicalStackFrames(this StackTrace trace) {
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
// 1. Capture the physical stack at the definition site.
            var trace = new StackTrace(true);
            var logicalStack = trace.GetLogicalStackFrames();

            // 2. Wrap the entire operation in a Using block that sets the AsyncLocal context.
            return Observable.Using(
                () => {
                    var originalStack = FaultHub.LogicalStackContext.Value;
                    FaultHub.LogicalStackContext.Value = logicalStack;
                    return Disposable.Create(() => FaultHub.LogicalStackContext.Value = originalStack);
                },
                _ => {
                    // 3. The rest of the implementation now works correctly because NewFaultContext()
                    //    will read the logicalStack we just placed in the AsyncLocal.
                    var faultContext = context.NewFaultContext(caller);
                    var contextName = faultContext.CustomContext.FirstOrDefault() ?? "Unknown";

                    return Observable.Defer(() => {
                        var parentHandlers = FaultHub.HandlersContext.Value;
                        return Observable.Using(
                                () => {
                                    var stack = ContextStack.Value ??= new Stack<object>();
                                    var token = new object();
                                    stack.Push(token);
                                    FaultHub.HandlersContext.Value = new List<Func<Exception, FaultAction?>>();
                                    $"ChainFaultContext('{contextName}') - Using Setup (Scope Cleared)".LogAsyncLocalState();
                                    return Disposable.Create(stack, objects => {
                                        objects.Pop();
                                        FaultHub.HandlersContext.Value = parentHandlers;
                                        $"ChainFaultContext('{contextName}') - Using Dispose (Scope Restored)".LogAsyncLocalState();
                                    });
                                },
                                s => {
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
                                    return ex.HandleFaultContext<T>(contextName, faultContext);
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
            var logicalStack = FaultHub.LogicalStackContext.Value;
            return new AmbientFaultContext {
                LogicalStackTrace = logicalStack,
                CustomContext = memberName.YieldItem()
                    .Concat((context ?? []).Distinct().WhereNotDefault().Select(o => o.ToString()))
                    .WhereNotNullOrEmpty()
                    .ToArray()
            };
        }
        
        private static IObservable<T> HandleFaultContext<T>(this Exception ex, string contextName, AmbientFaultContext faultContext) {
            $"[HUB][HandleFaultContext] Entered for context '{contextName}'.".LogToConsole();
            var enrichedException = ex.ExceptionToPublish(faultContext);
            var (localAction, muteOnRethrow) = ex.GetFaultResult();
            $"[HUB][HandleFaultContext] Local handler check resulted in action: '{(localAction, muteOnRethrow)}'.".LogToConsole();

            $"[HUB][HandleFaultContext] Local handler check resulted in action: '{(localAction,muteOnRethrow)}'.".LogToConsole();
            if (ex.IsSkipped()) {
                $"[HUB][HandleFaultContext] {nameof(FaultHub.MuteForBus)} '{contextName}'.".LogToConsole();
                enrichedException.MuteForBus();
            }

            if (localAction == FaultResult.Complete) {
                "[HUB][HandleFaultContext] Honoring local 'Complete' action.".LogToConsole();
                if (!enrichedException.IsSkipped()){
                    enrichedException.Publish();
                }
                return Observable.Empty<T>();
            }
    
            if (localAction == FaultResult.Rethrow) {
                "[HUB][HandleFaultContext] Honoring local 'Rethrow' action.".LogToConsole();
                if (muteOnRethrow) {
                    enrichedException.MuteForBus();
                }
                return Observable.Throw<T>(enrichedException);
            }
            
            var stackDepth = ContextStack.Value?.Count ?? 0;
            $"[HUB][HandleFaultContext] Local action is 'Proceed'. Checking for nesting. Stack depth: {stackDepth}.".LogToConsole();
            if (stackDepth > 1) {
                "[HUB][HandleFaultContext] Nested context detected. Propagating error upwards.".LogToConsole();
                return Observable.Throw<T>(enrichedException);
            }
            
            "[HUB][HandleFaultContext] Outermost context. Propagating error by default.".LogToConsole();
            return Observable.Throw<T>(enrichedException);

            
        }
    }
}