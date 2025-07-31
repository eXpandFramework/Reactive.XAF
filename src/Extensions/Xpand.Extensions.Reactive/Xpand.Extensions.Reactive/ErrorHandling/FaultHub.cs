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

namespace Xpand.Extensions.Reactive.ErrorHandling {

    public class AmbientFaultContext {
        public StackTrace DefinitionStackTrace { get; init; }
        public IReadOnlyList<string> CustomContext { get; init; }
    }

    public static class FaultHub {
        public static readonly AsyncLocal<AmbientFaultContext> CurrentContext = new();
        internal static readonly AsyncLocal<List<Func<Exception, FaultAction?>>> HandlersContext = new();
        static readonly AsyncLocal<Guid?> Ctx = new();
        static readonly Subject<Exception> PreRaw = new();
        static readonly Subject<Exception> MainRaw = new();
        public static readonly MemoryCache Seen = new(new MemoryCacheOptions { SizeLimit = 10000 });
        public static readonly ISubject<Exception> PreBus = Subject.Synchronize(PreRaw);
        public static readonly ISubject<Exception> Bus = Subject.Synchronize(MainRaw);
        const string KeyCId = "CorrelationId";
        public const string SkipKey = "FaultHub.Skip";
        const string PublishedKey = "FaultHub.Published";
        
        public static IObservable<T> ChainFaultContext<T>(this IObservable<T> source, Func<IObservable<T>, IObservable<T>> retryStrategy,
            object[] context = null, [CallerMemberName] string caller = "") {
    
            var faultContext = context.NewFaultContext(caller);
            var contextName = faultContext.CustomContext.FirstOrDefault() ?? "Unknown";
            Console.WriteLine($"[HUB][WithFaultContext] Operator defined in '{contextName}'. Applying retry strategy.");

            return retryStrategy(source).Catch((Exception ex) => ex.HandleFaultContext<T>(contextName, faultContext));
        }

        public static IObservable<T> ChainFaultContext<T>(this IObservable<T> source, object[] context, [CallerMemberName] string caller = "") {
            var faultContext = context.NewFaultContext(caller);
            var contextName = faultContext.CustomContext.FirstOrDefault() ?? "Unknown";
            Console.WriteLine($"[HUB][WithFaultContext] Operator defined in '{contextName}'.");
            return source.Catch((Exception ex) => ex.HandleFaultContext<T>(contextName, faultContext));
        }

        public static IObservable<T> ChainFaultContext<T>(this IObservable<T> source, 
            bool handleUpstreamRetries, object[] context = null, [CallerMemberName] string caller = "")
            => !handleUpstreamRetries ? source.ChainFaultContext(context, caller)
                : source.ChainFaultContext(errors => errors.Take(1).IgnoreElements(), context, caller);

        public static IObservable<T> ChainFaultContext<T>(this IObservable<T> source, [CallerMemberName] string caller = "")
            => source.ChainFaultContext([],caller);

        private static IObservable<T> HandleFaultContext<T>(this Exception ex,string contextName,  AmbientFaultContext faultContext){
            Console.WriteLine($"[HUB][WithFaultContext][{contextName}] Caught exception. Enriching and re-throwing.");
            var (action, muteOnRethrow) = ex.GetFaultResult();
            var enrichedException = ex.ExceptionToPublish(faultContext);
        
            if (ex.IsSkipped()) {
                enrichedException.MuteForBus();
            }

            switch (action) {
                case FaultResult.Complete:
                    enrichedException.Publish();
                    return Observable.Empty<T>();
            
                default: 
                    if (muteOnRethrow) {
                        enrichedException.MuteForBus();
                    }
                    return Observable.Throw<T>(enrichedException);
            }
        }
        
        private static AmbientFaultContext NewFaultContext(this object[] context, string caller) 
            => new() {
                DefinitionStackTrace = new StackTrace(2, true), CustomContext = caller.YieldItem()
                    .Concat((context ?? []).Distinct().WhereNotDefault().Select(o => o.ToString()))
                    .WhereNotNullOrEmpty()
                    .ToArray()
            };

        public static bool IsSkipped(this Exception exception) => exception.AccessData(data => data.Contains(SkipKey));

        public static bool IsPublished(this Exception exception)
            => exception.AccessData(data => data.Contains(PublishedKey));

        public static T MuteForBus<T>(this T ex) where T:Exception{
            ex.AccessData(data => data[SkipKey] = true);
            return ex;
        }

        public static Guid? CorrelationId(this Exception ex) => (Guid?)ex.AccessData(data => data[KeyCId]);

        private enum PublishAction {
            Continue,
            StopAndReturnTrue,
            StopAndReturnFalse
        }

        public enum FaultResult {
            Proceed,
            Complete,
            Rethrow
        }
        public static (FaultResult Action, bool Mute) GetFaultResult(this Exception originalException) {
            var handlerAction = HandlersContext.Value?.Select(handler => handler(originalException)).FirstOrDefault(action => action.HasValue);
            if (!handlerAction.HasValue) {
                return (FaultResult.Proceed, false);
            }
            Console.WriteLine($"[HUB] A handler matched with action: {handlerAction.Value}.");
            return handlerAction.Value switch {
                FaultAction.Complete => (FaultResult.Complete, false),
                FaultAction.Rethrow => (FaultResult.Rethrow, true),
                _ => (FaultResult.Proceed, false)
            };
        }

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
                    Console.WriteLine("[HUB][Publish] SkipKey found. Returning false."); 
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

        public static IObservable<T> Publish<T>(this Exception ex) {
            var publish = ex.Publish();
            Console.WriteLine($"[HUB][Publish<T>] Publish() returned: {publish}.");
            return publish ? Observable.Empty<T>() : Observable.Throw<T>(ex);
        }

        public static IObservable<T> MakeResilient<T>(this IObservable<T> source, AmbientFaultContext faultContext = null) {
            return source.Catch<T, Exception>(e => {
                Console.WriteLine($"[HUB][MakeResilient] Caught exception: '{e.Message}'. Enriching with context...");
                var (action, muteOnRethrow) = e.GetFaultResult();
                var exceptionToPublish = e.ExceptionToPublish(faultContext);
                if (e.IsSkipped()) {
                    exceptionToPublish.MuteForBus();
                }

                switch (action) {
                    case FaultResult.Complete:
                        exceptionToPublish.Publish();
                        return Observable.Empty<T>();
        
                    case FaultResult.Rethrow:
                        if (muteOnRethrow) {
                            exceptionToPublish.MuteForBus();
                        }
                        return Observable.Throw<T>(exceptionToPublish);
        
                    case FaultResult.Proceed:
                    default:
                        Console.WriteLine("[HUB][MakeResilient] Throwing enriched exception for publish/retry.");
                        return Observable.Throw<T>(exceptionToPublish);
                }
            });
        }
        public static IObservable<T> PublishFaults<T>(this IObservable<T> source) {
            return source.Catch<T, Exception>(ex => {
                Console.WriteLine($"[HUB][PublishFaults] Caught final exception: {ex.GetType().Name}. Attempting to publish.");
        
                // This now uses the helper which correctly re-throws muted exceptions.
                return ex.Publish<T>();
            });
        }        
        private static Exception ReOrderContext(this Exception e)
            => e is not FaultHubException faultHubException
                ? e
                : new FaultHubException(
                    faultHubException.Message,
                    faultHubException.InnerException,
                    new AmbientFaultContext {
                        DefinitionStackTrace = faultHubException.Context.DefinitionStackTrace,
                        CustomContext = faultHubException.Context.CustomContext.MoveFirstToEnd()
                    });

        private static Exception ExceptionToPublish(this Exception e, AmbientFaultContext contextToUse)
            => contextToUse == null ? e : e is not FaultHubException faultHubException
                    ? new FaultHubException("An exception occurred in a traced fault context.", e, contextToUse)
                    : new FaultHubException(faultHubException.Message, faultHubException.InnerException,
                        new AmbientFaultContext {
                            DefinitionStackTrace = contextToUse.DefinitionStackTrace,
                            CustomContext = faultHubException.Context.CustomContext.Concat(contextToUse.CustomContext)
                                .Distinct().ToArray()
                        });


        public static IDisposable Disable() => AddHandler(_ => FaultAction.Rethrow);

        public static IDisposable AddHandler(this Func<Exception, FaultAction?> handler) {
            var context = HandlersContext.Value ??= new List<Func<Exception, FaultAction?>>();
            context.Add(handler);
            return Disposable.Create(context, list => list.Remove(handler));
        }

    }
    public class FaultHubException(string message, Exception innerException, AmbientFaultContext context)
        : Exception(message, innerException) {
        public AmbientFaultContext Context { get; } = context;

        public override string ToString() {
            var builder = new StringBuilder();
            var frame = Context.DefinitionStackTrace.GetFrame(0);
            builder.AppendLine(
                $"--- Origin: {System.IO.Path.GetFileName(frame?.GetFileName())}:line {frame?.GetFileLineNumber()} ---");
            if (Context.CustomContext.Any()) {
                builder.AppendLine($"--- Context: {string.Join(" | ", Context.CustomContext)} ---");
            }

            builder.AppendLine("--- Original Exception ---");
            builder.AppendLine(InnerException?.ToString() ?? base.ToString());
            builder.AppendLine("--- Definition Site Stack Trace ---");
            builder.AppendLine(Context.DefinitionStackTrace.ToString());

            return builder.ToString();
        }
    }

}