using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using Microsoft.Extensions.Caching.Memory;
using Xpand.Extensions.ExceptionExtensions;
using Xpand.Extensions.MemoryCacheExtensions;
using Xpand.Extensions.Reactive.Utility;
using static Xpand.Extensions.Reactive.ErrorHandling.FaultHub.FaultHubLogger;

namespace Xpand.Extensions.Reactive.ErrorHandling.FaultHub {
    public static class FaultHub {
        public static readonly AsyncLocal<IReadOnlyList<LogicalStackFrame>> LogicalStackContext = new();
        internal static readonly AsyncLocal<List<Func<Exception, FaultAction?>>> HandlersContext = new();
        static readonly AsyncLocal<Guid?> Ctx = new();
        internal static readonly AsyncLocal<FaultSnapshot> CurrentFaultSnapshot = new();
        internal static readonly IAsyncLocal[] All= [LogicalStackContext.Wrap(), HandlersContext.Wrap(), Ctx.Wrap(), CurrentFaultSnapshot.Wrap()];
        public static readonly MemoryCache Seen = new(new MemoryCacheOptions { SizeLimit = 10000 });
        public static readonly ISubject<Exception> Bus = Subject.Synchronize(new Subject<Exception>());
        const string KeyCId = "CorrelationId";
        public const string SkipKey = "FaultHub.Skip";
        const string PublishedKey = "FaultHub.Published";
        private const string Key = "Origin";

        static FaultHub() => Logging = true;
        public static bool Enabled { get; set; } = true;
        public static void Reset() {
            LogicalStackContext.Value = null;
            HandlersContext.Value = null;
            CurrentFaultSnapshot.Value=null;
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
            Log(() => $"[HUB] A handler matched with action: {handlerAction.Value}.");
            return handlerAction.Value switch {
                FaultAction.Complete => (FaultResult.Complete, false),
                FaultAction.Rethrow => (FaultResult.Rethrow, false),
                _ => (FaultResult.Proceed, false)
            };
        }

        public static bool Publish(this Exception ex) {
            if (ex is FaultHubException fault) {
                Log(() => $"[HUB-TRACE][Publish] Publishing with final context: '{string.Join(", ", fault.Context.UserContext)}'");
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
                    Log(() => "[HUB][Publish] SkipKey found. Returning false."); 
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
            Log(() => $"[HUB][Publish<T>] Publish() returned: {publish}.");
            return publish ? Observable.Empty<T>() : Observable.Throw<T>(ex);
        }


        public static IObservable<T> PublishFaults<T>(this IObservable<T> source) 
            => Enabled ? source.Catch<T, Exception>(ex => {
                Log(() => $"[HUB][PublishFaults] Caught final exception: {ex.GetType().Name}. Attempting to publish.");
                return ex.ExceptionToPublish().Publish<T>();
            }) : source;

        public static bool Logging { get; set; }

        public static IObservable<T> CatchAndCompleteOnFault<T>(this IObservable<T> source, object[] context)
            => source.SwitchOnFault(ex => {
                ex.Publish();
                return Observable.Empty<T>();
            }, null, context);
        
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
            Func<IObservable<TSource>, IObservable<TSource>> retryStrategy = null, object[] context = null)
            => source.ChainFaultContext(retryStrategy, context)
                .Select(t => (TResult)(object)t).Catch(fallbackSelector);
        
        internal static IObservable<T> ProcessFault<T>(this Exception ex, AmbientFaultContext faultContext, Func<FaultHubException, IObservable<T>> proceedAction) {
            Log(() => $"[HUB][{nameof(ProcessFault)}] Entered for context '{faultContext.Name}'.");
            var enrichedException = ex.ExceptionToPublish(faultContext);
            var (localAction, muteOnRethrow) = ex.GetFaultResult();
            Log(() => $"[HUB][{nameof(ProcessFault)}] Local handler check resulted in action: '{(localAction, muteOnRethrow)}'.");

            if (ex.IsSkipped()) {
                Log(() => $"[HUB][{nameof(ProcessFault)}] {nameof(MuteForBus)} '{faultContext.Name}'.");
                enrichedException.MuteForBus();
            }

            if (localAction == FaultResult.Complete) {
                Log(() => $"[HUB][{nameof(ProcessFault)}] Honoring local 'Complete' action.");
                if (!enrichedException.IsSkipped()) {
                    enrichedException.Publish();
                }
                return Observable.Empty<T>();
            }

            if (localAction == FaultResult.Rethrow) {
                Log(() => $"[HUB][{nameof(ProcessFault)}] Honoring local 'Rethrow' action.");
                if (muteOnRethrow) {
                    enrichedException.MuteForBus();
                }
                return Observable.Throw<T>(enrichedException);
            }
            return proceedAction(enrichedException);
        }
    
        internal static object[] AddToContext(this object[] context, params object[] items) 
            => items.Concat(context ?? Enumerable.Empty<object>()).ToArray();
        
        public static IObservable<T> RethrowOnFault<T>(this IObservable<T> source, Func<Exception, bool> predicate = null) {
            predicate ??= _ => true;
            return source.RegisterHandler(ex => predicate(ex) ? FaultAction.Rethrow : null);
        }

        public static FaultHubException ExceptionToPublish(this Exception e, AmbientFaultContext contextToUse=null) {
            Log(() => "[HUB-TRACE][ExceptionToPublish] Entered.");
            
            if (contextToUse == null) {
                Log(() => "[HUB--TRACE][ExceptionToPublish] contextToUse is null, returning original exception.");
                var defaultContext = new System.Diagnostics.StackTrace(true).LogicalStackFrames().NewFaultContext([]);
                return e as FaultHubException ?? new FaultHubException("An exception occurred in a traced fault context.", e, defaultContext);

            }
            string incomingContextSummary;
            if (e is FaultHubException f) {
                incomingContextSummary = $"'{string.Join(", ", f.Context.UserContext)}'";
                if (contextToUse.UserContext is not { Length: > 0 } && contextToUse.LogicalStackTrace is not { Count: > 0 }) {
                    Log(() => $"[HUB-TRACE][ExceptionToPublish] Ignoring generic wrapper context '{contextToUse?.BoundaryName}'. Returning original FaultHubException.");
                    return f;
                }
            }
            else
                incomingContextSummary = "(none)";
            Log(() => $"[HUB-TRACE][ExceptionToPublish] Wrapping exception '{e.GetType().Name}'. Incoming Context: {incomingContextSummary}, New Context: '{string.Join(", ", contextToUse.UserContext)}'");
            
            

            if (e is not FaultHubException faultHubException) {
                Log(() => "[HUB-TRACE][ExceptionToPublish] Exception is not a FaultHubException. Creating new chain.");
                return new FaultHubException(e.Message, e, contextToUse);
            }
            Log(() => "[HUB-TRACE][ExceptionToPublish] Exception is already a FaultHubException. Chaining new context.");
            
            var newChainedContext = contextToUse with{ InnerContext = faultHubException.Context };
            var newMessage = $"{contextToUse.Name} failed";
            var newException = new FaultHubException(newMessage, faultHubException, newChainedContext);

            var finalContextSummary = $"'{string.Join(" | ", newException.Context.UserContext)}' -> '{string.Join(" | ", newException.Context.InnerContext?.UserContext ?? [])}'";
            Log(() => $"[HUB-TRACE][ExceptionToPublish] Created new FaultHubException. Final Context Chain: {finalContextSummary}");
    
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

    
    internal class FaultSnapshot {
        public IReadOnlyList<LogicalStackFrame> CapturedStack { get; set; }
    }
}