using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Caching.Memory;
using Xpand.Extensions.ExceptionExtensions;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.MemoryCacheExtensions;

namespace Xpand.Extensions.Reactive.ErrorHandling{
    
    public class AmbientFaultContext {
        public StackTrace DefinitionStackTrace { get; init; }
        public IReadOnlyList<string> CustomContext { get; init; }
    }
    
    public static class FaultHub {
        public static readonly AsyncLocal<string> DiagnosticContext = new();

        public static IDisposable EnterDiagnosticContext(string value) {
            var original = DiagnosticContext.Value;
            DiagnosticContext.Value = value;
            return Disposable.Create(DiagnosticContext,local => local.Value = original);
        }
        public static readonly AsyncLocal<AmbientFaultContext> CurrentContext = new();
        public static readonly AsyncLocal<bool> IsRetrying = new();
        internal static readonly AsyncLocal<List<Func<Exception, bool>>> HandlersContext = new();
        static readonly AsyncLocal<Guid?> Ctx = new();
        static readonly Subject<Exception> PreRaw  = new();
        static readonly Subject<Exception> MainRaw = new();
        public static readonly MemoryCache Seen = new(new MemoryCacheOptions { SizeLimit = 10000 });
        public static readonly ISubject<Exception> PreBus  = Subject.Synchronize(PreRaw);
        public static readonly ISubject<Exception> Bus     = Subject.Synchronize(MainRaw);
        const string KeyCId     = "CorrelationId";
        public const string SkipKey = "FaultHub.Skip";
        const string PublishedKey  = "FaultHub.Published";
        public static IObservable<T> WithFaultContext<T>(this IObservable<T> source, object[] context, Func<IObservable<T>, IObservable<T>> retrySelector, [CallerMemberName] string caller = "") 
            => source.MakeResilient(retrySelector, new AmbientFaultContext {
                DefinitionStackTrace =  new StackTrace(1, true),
                CustomContext =  caller.YieldItem().Concat(context.Distinct().WhereNotDefault().Select(o => o.ToString())).WhereNotNullOrEmpty().ToArray()
            });

        public static IObservable<T> WithFaultContext<T>(this IObservable<T> source, Func<IObservable<T>, IObservable<T>> retrySelector, [CallerMemberName] string caller = "") 
            => source.WithFaultContext([],retrySelector,caller);

        public static IObservable<T> WithFaultContext<T>(this IObservable<T> source, [CallerMemberName] string caller = "") 
            => source.WithFaultContext([],caller);

        public static IObservable<T> WithFaultContext<T>(this IObservable<T> source, object[] context,
            [CallerMemberName] string caller = "")
            => source.WithFaultContext(context,bus => bus,caller);

        static IObservable<T> MakeResilient<T>(this IObservable<T> source, Func<IObservable<T>, IObservable<T>> retrySelector = null, AmbientFaultContext faultContext = null)
            => Observable.Defer(() => {
                var isNestedRetry = IsRetrying.Value;
                Console.WriteLine($"[HUB] Defer entered. isNestedRetry = {isNestedRetry}");
                return source.StreamToCatch( retrySelector, isNestedRetry).Catch<T, Exception>(ex => {
                    Console.WriteLine($"[HUB] Catch block entered. The value of isNestedRetry is {isNestedRetry}");
                    var exceptionToPublish = ex.ExceptionToPublish( faultContext);
                    var handlers = HandlersContext.Value;
                    var handlersException = exceptionToPublish.HandlersException( handlers);
                    if (handlersException!=null) return Observable.Throw<T>(handlersException);
                    if (isNestedRetry) {
                        Console.WriteLine($"[HUB] isNestedRetry is TRUE. Re-throwing exception: {exceptionToPublish.Message}");
                        return Observable.Throw<T>(exceptionToPublish);
                    }
                    if (exceptionToPublish is FaultHubException faultHubException) {
                        exceptionToPublish = new FaultHubException(faultHubException.Message, faultHubException.InnerException, new AmbientFaultContext {
                            DefinitionStackTrace = faultHubException.Context.DefinitionStackTrace,
                            CustomContext = faultHubException.Context.CustomContext.MoveFirstToEnd()
                        });
                    }
                    Console.WriteLine($"[HUB] isNestedRetry is FALSE. Publishing exception to the bus: {exceptionToPublish.Message}");
                    return exceptionToPublish.Publish<T>();
                });
            });

        private static Exception HandlersException(this Exception exceptionToPublish, List<Func<Exception, bool>> handlers){
            var originalException = (exceptionToPublish as FaultHubException)?.InnerException ?? exceptionToPublish;
            if (handlers == null || !handlers.Any(handler => handler(originalException))) return null;
            Console.WriteLine("[HUB] A handler matched. Re-throwing exception.");
            return exceptionToPublish;

        }

        private static Exception ExceptionToPublish(this Exception ex, AmbientFaultContext faultContext){
            var contextToUse = faultContext ?? CurrentContext.Value;
            if (contextToUse == null) return ex;
            if (ex is not FaultHubException fhEx) return new FaultHubException("An exception occurred in a traced fault context.", ex, contextToUse);
            var newCustomContext = fhEx.Context.CustomContext.Concat(contextToUse.CustomContext).Distinct().ToArray();
            var mergedContext = new AmbientFaultContext {
                DefinitionStackTrace = contextToUse.DefinitionStackTrace, 
                CustomContext = newCustomContext
            };
            return new FaultHubException(fhEx.Message, fhEx.InnerException, mergedContext);

        }

        private static IObservable<T> StreamToCatch<T>(this IObservable<T> source, Func<IObservable<T>, IObservable<T>> retrySelector, bool isNestedRetry) 
            => retrySelector == null ? source : Observable.Defer(() => {
                    IsRetrying.Value = true;
                    return retrySelector(source);
                })
                .Finally(() => IsRetrying.Value = isNestedRetry);

        public static bool IsSkipped(this Exception exception) => exception.AccessData(data => data.Contains(SkipKey));

        public static bool IsPublished(this Exception exception) => exception.AccessData(data => data.Contains(PublishedKey));


        public static void MuteForBus(this Exception ex) => ex.AccessData(data => data[SkipKey]=true);
        
        public static Guid? CorrelationId(this Exception ex) => (Guid?)ex.AccessData(data => data[KeyCId]);
        
        private enum PublishAction { Continue, StopAndReturnTrue, StopAndReturnFalse }

        public static bool Publish(this Exception ex) {
            var (action, correlationId) = ex.AccessData(data => {
                if (data.Contains(PublishedKey)) {
                    return (PublishAction.StopAndReturnTrue, Guid.Empty);
                }
                data[PublishedKey] = new object();
                if (!data.Contains(KeyCId) && Ctx.Value.HasValue) {
                    data[KeyCId] = Ctx.Value;
                }
                if (data.Contains(SkipKey)) {
                    return (PublishAction.StopAndReturnFalse, Guid.Empty);
                }
                ex.TagOrigin();
                var id = data[KeyCId] as Guid? ?? Guid.Empty;
                return (PublishAction.Continue, id);
            });

            switch (action) {
                case PublishAction.StopAndReturnTrue:
                    return true; 
                case PublishAction.StopAndReturnFalse:
                    return false;
            }

            var deduplicationKey = $"{correlationId}:{ex.GetType().FullName}:{ex.Message}";
            if (correlationId != Guid.Empty && !Seen.TryAdd(deduplicationKey)) {
                return false;
            }

            PreRaw.OnNext(ex);
            MainRaw.OnNext(ex);
            return true;
        }

        public static IObservable<T> Publish<T>(this Exception ex) 
            => ex.Publish() ? Observable.Empty<T>() : Observable.Throw<T>(ex);
        
        public static IDisposable Disable() => AddHandler(_ => true);

        public static IDisposable AddHandler(Func<Exception, bool> handler) {
            var context = HandlersContext.Value ??= new List<Func<Exception, bool>>();
            context.Add(handler);
            return Disposable.Create(context,list => list.Remove(handler));
        }

        public static IObservable<T> ForceFaults<T>(this IObservable<T> source) 
            => Observable.Create<T>(observer => {
                var originalIsRetrying = IsRetrying.Value;
                IsRetrying.Value = false; 
                var subscription = source.Subscribe(observer);
                return new CompositeDisposable(subscription, Disposable.Create(IsRetrying,local => local.Value=originalIsRetrying));
            });

        public static IObservable<T> ExposeFaults<T>(this IObservable<T> source) {
            return Observable.Create<T>(observer => {
                var originalIsRetrying = IsRetrying.Value;
                IsRetrying.Value = true; 
                var subscription = source.Subscribe(observer);
                return new CompositeDisposable(subscription, Disposable.Create(IsRetrying,local => local.Value=originalIsRetrying));
            });
        }
    }
    
    public class FaultHubException(string message, Exception innerException, AmbientFaultContext context)
        : Exception(message, innerException) {
        public AmbientFaultContext Context { get; } = context;

        public override string ToString() {
            var builder = new StringBuilder();
            var frame = Context.DefinitionStackTrace.GetFrame(0);
            builder.AppendLine($"--- Origin: {System.IO.Path.GetFileName(frame?.GetFileName())}:line {frame?.GetFileLineNumber()} ---");
            if (Context.CustomContext.Any()) {
                builder.AppendLine($"--- Context: {string.Join(" | ", Context.CustomContext)} ---");
            }
            builder.AppendLine("--- Original Exception ---");
            builder.AppendLine(InnerException?.ToString() ?? base.ToString());
            builder.AppendLine("--- Definition Site Stack Trace ---");
            builder.AppendLine(Context.DefinitionStackTrace.ToString());
        
            return builder.ToString();
        }
    }}