using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Caching.Memory;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Reactive.ErrorHandling.FaultHub{
    public static class ChainFaultContextService {
        
        internal static readonly AsyncLocal<Stack<object>> ContextStack = new();
        
        private static readonly MemoryCache StackTraceStringCache = new(new MemoryCacheOptions());

        internal static string GetOrAddCachedStackTraceString() {
            var keyTrace = new StackTrace(false);
            StackFrame originFrame = null;
            var ourNamespace = typeof(ChainFaultContextService).Namespace;
            
            foreach (var frame in keyTrace.GetFrames()) {
                var method = frame.GetMethod();
                if (method?.DeclaringType != null && method.DeclaringType.Namespace != ourNamespace) {
                    originFrame = frame;
                    break;
                }
            }
            if (originFrame?.GetMethod() == null) {
                return "  Stack trace could not be reliably determined for this call site.";
            }
            var originMethod = originFrame.GetMethod();
            var key = $"{originMethod?.DeclaringType?.AssemblyQualifiedName}:{originMethod?.MetadataToken}:{originFrame.GetILOffset()}";

            return StackTraceStringCache.GetOrCreate(key, _ => {
                var fullTrace = new StackTrace(true);
                var stringBuilder = new StringBuilder();
                string[] reactiveNamespaces = ["System.Reactive."];

                var filteredFrames = fullTrace.GetFrames().Where(frame => {
                    var method = frame.GetMethod();
                    if (method?.DeclaringType == null) return false;
                    
                    var ns = method.DeclaringType.Namespace;
                    return ns != null && ns != ourNamespace && reactiveNamespaces.All(rns => !ns.StartsWith(rns));
                });

                foreach (var frame in filteredFrames) {
                    stringBuilder.AppendLine($"   at {frame.ToString().Trim()}");
                }
                
                return stringBuilder.ToString().TrimEnd();
            });
        }
        public static IObservable<T> ChainFaultContext<T>(this IObservable<T> source, Func<IObservable<T>, IObservable<T>> retryStrategy,
            object[] context = null, [CallerMemberName] string caller = "")
            => (retryStrategy != null ? retryStrategy(source) : source).ChainFaultContext(context, caller);

        public static IObservable<T> ChainFaultContext<T>(this IObservable<T> source, 
            bool handleUpstreamRetries, object[] context = null, [CallerMemberName] string caller = "")
            => !handleUpstreamRetries ? source.ChainFaultContext(context, caller)
                : source.ChainFaultContext(errors => errors.Take(1).IgnoreElements(), context, caller);

        public static IObservable<T> ChainFaultContext<T>(this IObservable<T> source, [CallerMemberName] string caller = "")
            => source.ChainFaultContext([],caller);

        private static void LogAsyncLocalState(this string step) {
            var handlerCount = FaultHub.HandlersContext.Value?.Count ?? -1;
            var nestingDepth = ContextStack.Value?.Count ?? -1;
            $"[HUB-DIAGNOSTIC][{step}] Handlers: {handlerCount}, Nesting: {nestingDepth}".LogToConsole();
        }
        public static IObservable<T> ChainFaultContext<T>(this IObservable<T> source, object[] context,
            [CallerMemberName] string caller = "") {
            var faultContext = context.NewFaultContext(caller);
            var contextName = faultContext.CustomContext.FirstOrDefault() ?? "Unknown";

            return Observable.Defer(() => {
                    $"ChainFaultContext('{contextName}') - Defer Entry".LogAsyncLocalState();
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
                            _ => {
                                $"[HUB][WithFaultContext] Operator defined in '{contextName}'.".LogToConsole();
                                return source;
                            }
                        )
                        .Catch((Exception ex) => {
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
            
        }
        

        internal static void LogToConsole(this string message) {
            if (FaultHub.Logging) Console.WriteLine(message);
        }

        public static AmbientFaultContext NewFaultContext(this object[] context, string caller) {
            $"[HUB-TRACE][NewFaultContext] Caller: '{caller}', Context: '{(context == null ? "null" : string.Join(", ", context))}'".LogToConsole();
            return new AmbientFaultContext {
                InvocationStackTrace = GetOrAddCachedStackTraceString(), 
                CustomContext = caller.YieldItem()
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