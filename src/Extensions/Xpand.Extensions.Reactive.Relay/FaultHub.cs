using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using Microsoft.Extensions.Caching.Memory;
using Xpand.Extensions.ExceptionExtensions;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.MemoryCacheExtensions;
using Xpand.Extensions.Reactive.Relay.Transaction;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Reactive.Relay {
    public static class FaultHub {
        internal const string CapturedStackKey = "FaultHub.CapturedStack";
        public static readonly AsyncLocal<IReadOnlyList<LogicalStackFrame>> LogicalStackContext=NewContext<IReadOnlyList<LogicalStackFrame>>();
        internal static readonly AsyncLocal<List<Func<Exception, FaultAction?>>> HandlersContext=NewContext<List<Func<Exception, FaultAction?>>>();
        static readonly AsyncLocal<Guid?> Ctx=NewContext<Guid?>();
        public static readonly AsyncLocal<FaultSnapshot> CurrentFaultSnapshot=NewContext<FaultSnapshot>();
        public static readonly MemoryCache Seen = new(new MemoryCacheOptions { SizeLimit = 10000 });
        static readonly ISubject<FaultHubException> BusSubject = Subject.Synchronize(new Subject<FaultHubException>());
        public static readonly IObservable<FaultHubException> Bus = BusSubject.AsObservable();
        public static Dictionary<string, string> BlacklistedFilePathRegexes { get; } = new() {
            [@"Reactive.XAF"] = "Xpand Framework"
        };
        
        const string KeyCId = "CorrelationId";
        public const string SkipKey = "FaultHub.Skip";
        const string PublishedKey = "FaultHub.Published";
        private const string Key = "Origin";

        static FaultHub() => Logging = true;
        public static bool Enabled { get; set; } = true;
        public static void Reset() {
            RegisteredContexts
                .Do(local => local.Value=null)
                .Enumerate();
            Seen.Clear();
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
            if (!handlerAction.HasValue) return (FaultResult.Proceed, false);
            LogFast($"[HUB] A handler matched with action: {handlerAction.Value}.");
            return handlerAction.Value switch {
                FaultAction.Complete => (FaultResult.Complete, false),
                FaultAction.Rethrow => (FaultResult.Rethrow, false),
                _ => (FaultResult.Proceed, false)
            };
        }

        public static bool Publish(this FaultHubException ex) {
            
            var (action, correlationId) = ex.EvaluatePublishCriteria();
            switch (action) {
                case PublishAction.StopAndReturnTrue: return true;
                case PublishAction.StopAndReturnFalse: return false;
            }
            if (correlationId != Guid.Empty && !Seen.TryAdd($"{correlationId}:{ex.GetType().FullName}:{ex.Message}")) return false;
            try {
                BusSubject.OnNext(ex);
                LogFast($"[DIAGNOSTIC][Publish] ==> Bus.OnNext(ex) completed successfully.");
            }
            catch (Exception busEx) {
                LogFast($"[DIAGNOSTIC][Publish] ==> CRITICAL: Exception thrown from Bus.OnNext(): {busEx.Message}");
                throw;
            }
            return true;
        }

        private static (PublishAction action, Guid correlationId) EvaluatePublishCriteria(this Exception ex) => ex.AccessData(data => {
            if (data.Contains(PublishedKey)) return (PublishAction.StopAndReturnTrue, Guid.Empty);
            data[PublishedKey] = new object();
            if (!data.Contains(KeyCId) && Ctx.Value.HasValue) data[KeyCId] = Ctx.Value;
            if (data.Contains(SkipKey)) return (PublishAction.StopAndReturnFalse, Guid.Empty);
            ex.TagOrigin();
            return (PublishAction.Continue, (data[KeyCId] as Guid? ?? Guid.Empty));
        });

        public static IObservable<T> Publish<T>(this FaultHubException ex) {
            var publish = ex.Publish();
            LogFast($"[HUB][Publish<T>] Publish() returned: {publish}.");
            return publish ? Observable.Empty<T>() : Observable.Throw<T>(ex);
        }
        
        public static IObservable<T> PublishFaults<T>(this IObservable<T> source) 
            => Enabled ? source.Catch<T, Exception>(ex => {
                    LogFast($"[DIAGNOSTIC][PublishFaults] ==> Caught exception of type: {ex.GetType().Name}");
                    var faultToPublish = ex as FaultHubException ?? ex.ExceptionToPublish();
                    LogFast($"[DIAGNOSTIC][PublishFaults] ==> Fault to be published is of type: {faultToPublish.GetType().Name}");
                    var published = faultToPublish.Publish();
                    LogFast($"[DIAGNOSTIC][PublishFaults] ==> Call to internal Publish() method returned: {published}");
                    return published ? Observable.Empty<T>() : Observable.Throw<T>(ex);
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
                if (mute) ex.MuteForBus();
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
            => source.ChainFaultContext( retryStrategy, context).Select(t => (TResult)(object)t).Catch(fallbackSelector);
        
        internal static IObservable<T> ProcessFault<T>(this Exception ex, AmbientFaultContext faultContext, Func<FaultHubException, IObservable<T>> proceedAction) {
            LogFast($"[HUB][{nameof(ProcessFault)}] Entered for context '{faultContext.Name}'.");
            var enrichedException = ex.ExceptionToPublish(faultContext);
            var (localAction, muteOnRethrow) = ex.GetFaultResult();
            LogFast($"[HUB][{nameof(ProcessFault)}] Local handler check resulted in action: '{(localAction, muteOnRethrow)}'.");
            if (ex.IsSkipped()) {
                LogFast($"[HUB][{nameof(ProcessFault)}] {nameof(MuteForBus)} '{faultContext.Name}'.");
                enrichedException.MuteForBus();
            }
            return enrichedException.ProcessFault(proceedAction, localAction,  muteOnRethrow);
        }

        private static IObservable<T> ProcessFault<T>(this FaultHubException e,Func<FaultHubException, IObservable<T>> proceedAction, FaultResult localAction, bool muteOnRethrow){
            switch (localAction) {
                case FaultResult.Complete:
                    LogFast($"[HUB][{nameof(ProcessFault)}] Honoring local 'Complete' action.");
                    if (!e.IsSkipped()) {
                        e.Publish();
                    }
                    return Observable.Empty<T>();

                case FaultResult.Rethrow:
                    LogFast($"[HUB][{nameof(ProcessFault)}] Honoring local 'Rethrow' action.");
                    if (muteOnRethrow) {
                        e.MuteForBus();
                    }
                    return Observable.Throw<T>(e);

                case FaultResult.Proceed:
                default:
                    return proceedAction(e);
            }
        }

        internal static object[] AddToContext(this object[] context, params object[] items) 
            => items.WhereNotDefault().Concat(context ?? Enumerable.Empty<object>()).ToArray();
        
        public static IObservable<T> RethrowOnFault<T>(this IObservable<T> source, Func<Exception, bool> predicate = null) {
            predicate ??= _ => true;
            return source.RegisterHandler(ex => predicate(ex) ? FaultAction.Rethrow : null);
        }

        public static FaultHubException ExceptionToPublish(this Exception exception, object[] context, IReadOnlyList<string> tags, string memberName){
            var stack = exception.CapturedStack();
            var capturedStack = stack ?? LogicalStackContext.Value;
            var contextForStep = capturedStack.NewFaultContext( context, tags: tags, memberName: memberName);
            if (exception is TransactionAbortedException abortedException) {
                contextForStep = contextForStep with { BoundaryName = abortedException.Context.BoundaryName };
            }
            LogFast($"[INSTRUMENTATION][CollectErrors] Creating context for step '{memberName}'. Tags are: [{string.Join(", ", contextForStep.Tags)}]");
            return exception.ExceptionToPublish(contextForStep);
        }

        public static FaultHubException ExceptionToPublish(this Exception e, AmbientFaultContext contextToUse=null) {
            LogFast($"[FaultHub.ExceptionToPublish] Entered. Exception Type: {e.GetType().Name}. Context to use: '{contextToUse?.BoundaryName ?? "null"}'");
            if (contextToUse == null) {
                LogFast($"[FaultHub.ExceptionToPublish] contextToUse is null, returning original exception.");
                var existingFault = e.SelectMany().OfType<FaultHubException>().FirstOrDefault();
                if (existingFault != null) return existingFault;
                return e as FaultHubException ?? new FaultHubException("An exception occurred in a traced fault context.", e, new System.Diagnostics.StackTrace(true).LogicalStackFrames().NewFaultContext([]));
            }
            if (e is not FaultHubException faultHubException) {
                var message = e is AggregateException ? $"{contextToUse.BoundaryName} completed with errors" : e.Message;
                LogFast($"[FaultHub.ExceptionToPublish] Path: Raw exception. Wrapping in new FaultHubException.");
                return new FaultHubException(message, e, contextToUse);
            }
            LogFast($"[FaultHub.ExceptionToPublish] Path: Existing FaultHubException. Chaining context.");
            
            var newChainedContext = contextToUse with{ InnerContext = faultHubException.Context };
            
            if (faultHubException.PreserveType) {
                LogFast($"[FaultHub.ExceptionToPublish] Path: PreserveType is true for {faultHubException.GetType().Name}. Re-creating instance.");
                return (FaultHubException)Activator.CreateInstance(faultHubException.GetType(),
                    faultHubException.Message, faultHubException, newChainedContext);
            }
            
            
            LogFast($"[FaultHub.ExceptionToPublish] Path: PreserveType is false. Creating standard FaultHubException wrapper.");
            var newException = new FaultHubException(faultHubException.Message, faultHubException, newChainedContext);
            var finalContextSummary = $"'{string.Join(" | ", newException.Context.UserContext)}' -> '{string.Join(" | ", newException.Context.InnerContext?.UserContext ?? [])}'";
            LogFast($"[FaultHub.ExceptionToPublish] Created new FaultHubException. Final Context Chain: {finalContextSummary}");
            return newException;
        }
        public static IDisposable Disable() => AddHandler(_ => FaultAction.Rethrow);

        public static IDisposable AddHandler(this Func<Exception, FaultAction?> handler) {
            var context = HandlersContext.Value ??= [];
            context.Add(handler);
            return Disposable.Create(context, list => list.Remove(handler));
        }

        public static IReadOnlyList<LogicalStackFrame> CapturedStack(this Exception exception) 
            => exception.Data.Contains(CapturedStackKey) ? (IReadOnlyList<LogicalStackFrame>)exception.Data[CapturedStackKey] : null;

        internal static IObservable<T> FaultHubFlowContext<T>(this IObservable<T> source,
            Func<IObservable<T>, IObservable<T>> retrySelector = null, params IAsyncLocal[] context)
            => source.FlowContext(retrySelector, RegisteredContexts.Concat(context).ToArray());
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


    public class FaultSnapshot {
        public IReadOnlyList<LogicalStackFrame> CapturedStack { get; set; }
    }
}