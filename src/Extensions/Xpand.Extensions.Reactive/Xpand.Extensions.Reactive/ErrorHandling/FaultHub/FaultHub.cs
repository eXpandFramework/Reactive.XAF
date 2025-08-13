using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Extensions.Caching.Memory;
using Xpand.Extensions.ExceptionExtensions;
using Xpand.Extensions.MemoryCacheExtensions;
using Xpand.Extensions.Reactive.Combine;

namespace Xpand.Extensions.Reactive.ErrorHandling.FaultHub {
    public static class FaultHub {
        public static readonly AsyncLocal<IReadOnlyList<LogicalStackFrame>> LogicalStackContext = new();
        internal static readonly AsyncLocal<List<Func<Exception, FaultAction?>>> HandlersContext = new();
        static readonly AsyncLocal<Guid?> Ctx = new();
        public static readonly MemoryCache Seen = new(new MemoryCacheOptions { SizeLimit = 10000 });
        public static readonly ISubject<Exception> Bus = Subject.Synchronize(new Subject<Exception>());
        const string KeyCId = "CorrelationId";
        public const string SkipKey = "FaultHub.Skip";
        const string PublishedKey = "FaultHub.Published";
        private const string Key = "Origin";

        public static void Reset() {
            LogicalStackContext.Value = null;
            HandlersContext.Value = null;
            Ctx.Value = null;
            Seen.Clear();
            ChainFaultContextService.ContextStack.Value = null;
        }

        public static Exception TagOrigin(this Exception ex)
            => ex.AccessData(data => {
                if (data.Contains(Key)) return ex;
                var trace = new System.Diagnostics.StackTrace(true);
                var logicalFrames = trace.LogicalStackFrames();
                var stackTraceString = string.Join(Environment.NewLine, logicalFrames.Select(f => f.ToString()));
                data[Key] = stackTraceString;
                return ex;
            });

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

        public static (FaultResult Action, bool Mute) GetFaultResult(this Exception originalException) {
            var handlerAction = HandlersContext.Value?.Select(handler => handler(originalException)).FirstOrDefault(action => action.HasValue);
            if (!handlerAction.HasValue) {
                return (FaultResult.Proceed, false);
            }
            $"[HUB] A handler matched with action: {handlerAction.Value}.".LogToConsole();
            return handlerAction.Value switch {
                FaultAction.Complete => (FaultResult.Complete, false),
                FaultAction.Rethrow => (FaultResult.Rethrow, true),
                _ => (FaultResult.Proceed, false)
            };
        }

        public static bool Publish(this Exception ex) {
            if (ex is FaultHubException fault) {
                $"[HUB-TRACE][Publish] Publishing with final context: '{string.Join(", ", fault.Context.CustomContext)}'".LogToConsole();
            }
            var (action, correlationId) = ex.AccessData(data => {
                if (data.Contains(PublishedKey)) {
                    return (PublishAction.StopAndReturnTrue, Guid.Empty);
                }

                data[PublishedKey] = new object();

                if (!data.Contains(KeyCId) && Ctx.Value.HasValue) {
                    data[KeyCId] = Ctx.Value;
                }

                if (data.Contains(SkipKey)) {
                    "[HUB][Publish] SkipKey found. Returning false.".LogToConsole(); 
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
            Bus.OnNext(ex);
            return true;
        }

        public static IObservable<T> Publish<T>(this Exception ex) {
            var publish = ex.Publish();
            $"[HUB][Publish<T>] Publish() returned: {publish}.".LogToConsole();
            return publish ? Observable.Empty<T>() : Observable.Throw<T>(ex);
        }


        public static IObservable<T> PublishFaults<T>(this IObservable<T> source) {
            return source.Catch<T, Exception>(ex => {
                $"[HUB][PublishFaults] Caught final exception: {ex.GetType().Name}. Attempting to publish.".LogToConsole();
                return ex.Publish<T>();
            });
        }

        public static bool Logging { get; set; }

        public static IObservable<T> CatchAndCompleteOnFault<T>(this IObservable<T> source, object[] context, [CallerMemberName] string caller = "")
            => source.SwitchOnFault(ex => {
                ex.Publish();
                return Observable.Empty<T>();
            }, null, context, caller);
        
        private static IObservable<T> RegisterHandler<T>(this IObservable<T> source, Func<Exception, FaultAction?> handler) 
            => Observable.Using(handler.AddHandler, _ => source);

        static IObservable<T> CompleteOnFault<T>(this IObservable<T> source, bool mute = true, Action<Exception> onError = null, Func<Exception, bool> match = null) {
            var predicate = match ?? (_ => true);

            FaultAction? Handler(Exception ex) {
                if (!predicate(ex)) return null;
                if (mute) {
                    ex.MuteForBus();
                }
                onError?.Invoke(ex);
                return FaultAction.Complete;
            }
            return source.RegisterHandler(Handler);
        }
        public static IObservable<T> CompleteOnFault<T>(this IObservable<T> source, Action<Exception> onError = null, Func<Exception, bool> match = null) 
            => source.CompleteOnFault(true,onError,match);

        public static IObservable<T> CompleteOnFault<T,TException>(this IObservable<T> source,Action<Exception> onError=null) where TException:Exception
            => source.CompleteOnFault(onError,exception => exception is TException);
      
        public static IObservable<T> CompleteOnFault<T>(this IObservable<T> source,Type exceptionType,Action<Exception> onError=null) 
            => source.CompleteOnFault(onError,exceptionType.IsInstanceOfType);
        public static IObservable<T> CompleteOnFault<T>(this IObservable<T> source, Func<Exception, bool> predicate)
            => source.CompleteOnFault(_ => { },predicate);
        public static IObservable<T> PublishOnFault<T>(this IObservable<T> source, Func<Exception, bool> predicate)
            => source.PublishOnFault(_ => {},predicate);
        public static IObservable<T> PublishOnFault<T>(this IObservable<T> source, Action<Exception> onError = null, Func<Exception, bool> match = null)
            => source.CompleteOnFault( false, onError,match);
        public static IObservable<TResult> SwitchOnFault<TSource, TResult>(this IObservable<TSource> source, Func<FaultHubException, IObservable<TResult>> fallbackSelector, 
            Func<IObservable<TSource>, IObservable<TSource>> retryStrategy = null, object[] context = null, [CallerMemberName] string caller = "")
            => source.ChainFaultContext(retryStrategy, context, caller)
                .Select(t => (TResult)(object)t).Catch(fallbackSelector);

        public static IObservable<T> MergeResilient<T>(this IEnumerable<IObservable<T>> sources) 
            => sources.Select(source => source.ContinueOnFault()).Merge();

        public static IObservable<T> MergeResilient<T>(params IObservable<T>[] sources) 
            => sources.AsEnumerable().MergeResilient();

        [Obsolete("add to contract")]
        public static IObservable<T> MergeResilient<T>(this IObservable<T> source ,IObservable<T> other) 
            => source.ContinueOnFault().Merge(other.ContinueOnFault());
        public static IObservable<Unit> MergeToUnitResilient<TSource,TValue>(this IObservable<TSource> source ,IObservable<TValue> other) 
            => source.ContinueOnFault().MergeToUnit(other.ContinueOnFault());

        internal static IObservable<T> ProcessFault<T>(this Exception ex, AmbientFaultContext faultContext, Func<FaultHubException, IObservable<T>> proceedAction) {
            $"[HUB][{nameof(ProcessFault)}] Entered for context '{faultContext.Name}'.".LogToConsole();
            var enrichedException = ex.ExceptionToPublish(faultContext);
            var (localAction, muteOnRethrow) = ex.GetFaultResult();
            $"[HUB][{nameof(ProcessFault)}] Local handler check resulted in action: '{(localAction, muteOnRethrow)}'.".LogToConsole();

            if (ex.IsSkipped()) {
                $"[HUB][{nameof(ProcessFault)}] {nameof(MuteForBus)} '{faultContext.Name}'.".LogToConsole();
                enrichedException.MuteForBus();
            }

            if (localAction == FaultResult.Complete) {
                $"[HUB][{nameof(ProcessFault)}] Honoring local 'Complete' action.".LogToConsole();
                if (!enrichedException.IsSkipped()) {
                    enrichedException.Publish();
                }
                return Observable.Empty<T>();
            }

            if (localAction == FaultResult.Rethrow) {
                $"[HUB][{nameof(ProcessFault)}] Honoring local 'Rethrow' action.".LogToConsole();
                if (muteOnRethrow) {
                    enrichedException.MuteForBus();
                }
                return Observable.Throw<T>(enrichedException);
            }
            return proceedAction(enrichedException);
        }
    
        internal static object[] AddToContext<T>(this object[] context, T item) 
            => new object[] { item }.Concat(context ?? Enumerable.Empty<object>()).ToArray();
        
        public static IObservable<T> RethrowOnFault<T>(this IObservable<T> source, Func<Exception, bool> predicate = null) {
            predicate ??= _ => true;
            return source.RegisterHandler(ex => predicate(ex) ? FaultAction.Rethrow : null);
        }

        public static FaultHubException ExceptionToPublish(this Exception e, AmbientFaultContext contextToUse) {
            "[HUB-TRACE][ExceptionToPublish] Entered.".LogToConsole();
            if (contextToUse == null) {
                "[HUB--TRACE][ExceptionToPublish] contextToUse is null, returning original exception.".LogToConsole();
                return e as FaultHubException ?? new FaultHubException("An exception occurred in a traced fault context.", e, new AmbientFaultContext());
            }

            var incomingContextSummary = e is FaultHubException f ? $"'{string.Join(", ", f.Context.CustomContext)}'" : "(none)";
            $"[HUB-TRACE][ExceptionToPublish] Wrapping exception '{e.GetType().Name}'. Incoming Context: {incomingContextSummary}, New Context: '{string.Join(", ", contextToUse.CustomContext)}'".LogToConsole();

            if (e is not FaultHubException faultHubException) {
                "[HUB-TRACE][ExceptionToPublish] Exception is not a FaultHubException. Creating new chain.".LogToConsole();
                return new FaultHubException("An exception occurred in a traced fault context.", e, contextToUse);
            }
            "[HUB-TRACE][ExceptionToPublish] Exception is already a FaultHubException. Chaining new context.".LogToConsole();
            
            var newChainedContext = contextToUse with{ InnerContext = faultHubException.Context };
            var newException = new FaultHubException(faultHubException.Message, faultHubException.InnerException, newChainedContext);
            var finalContextSummary = $"'{string.Join(" | ", newException.Context.CustomContext)}' -> '{string.Join(" | ", newException.Context.InnerContext?.CustomContext ?? [])}'";
            $"[HUB-TRACE][ExceptionToPublish] Created new FaultHubException. Final Context Chain: {finalContextSummary}".LogToConsole();
    
            return newException;
        }
        public static IDisposable Disable() => AddHandler(_ => FaultAction.Rethrow);

        public static IDisposable AddHandler(this Func<Exception, FaultAction?> handler) {
            var context = HandlersContext.Value ??= [];
            context.Add(handler);
            return Disposable.Create(context, list => list.Remove(handler));
        }
    }

    public enum FaultResult {
        Proceed,
        Complete,
        Rethrow
    }
    
    public enum FaultAction {
        Rethrow,
        Complete
    }

}