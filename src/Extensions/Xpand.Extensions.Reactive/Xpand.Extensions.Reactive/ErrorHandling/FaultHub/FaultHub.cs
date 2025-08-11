using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Caching.Memory;
using Xpand.Extensions.ExceptionExtensions;
using Xpand.Extensions.MemoryCacheExtensions;

namespace Xpand.Extensions.Reactive.ErrorHandling.FaultHub {
    public static class FaultHub {
        internal static readonly AsyncLocal<IReadOnlyList<LogicalStackFrame>> LogicalStackContext = new();
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

            PreRaw.OnNext(ex);
            MainRaw.OnNext(ex);
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
        // public static IObservable<Unit> SwitchOnFault<TSource>(this IObservable<TSource> source, 
        //     Func<IObservable<TSource>, IObservable<TSource>> retryStrategy = null, object[] context = null, [CallerMemberName] string caller = "")
        //     => source.SwitchOnFault(_ => Unit.Default.Observe(),retryStrategy, context, caller);
        
        public static IObservable<T> ContinueOnFault<T>(this IObservable<T> source, object[] context=null,[CallerMemberName]string memberName="",[CallerFilePath]string filePath="",[CallerLineNumber]int lineNumber=0) 
            => source.PushStackFrame(memberName, filePath, lineNumber)
                .Catch((Exception ex) => {
                    ex.ExceptionToPublish(context.NewFaultContext(memberName, filePath, lineNumber)).Publish();
                    return Observable.Empty<T>();
                });
        public static IObservable<T> ContinueOnFault<T>(this IObservable<T> source,
            Func<IObservable<T>, IObservable<T>> retryStrategy, object[] context = null,
            [CallerMemberName]string memberName="",[CallerFilePath]string filePath="",[CallerLineNumber]int lineNumber=0) {
            var stream = retryStrategy != null ? retryStrategy(source) : source;
            return stream.PushStackFrame(memberName, filePath, lineNumber)
                .Catch((Exception ex) => {
                    ex.ExceptionToPublish(context.NewFaultContext(memberName, filePath, lineNumber)).Publish();
                    return Observable.Empty<T>();
                });
        }

        public static IObservable<T> RethrowOnFault<T>(this IObservable<T> source, Func<Exception, bool> predicate = null) {
            predicate ??= _ => true;
            return source.RegisterHandler(ex => predicate(ex) ? FaultAction.Rethrow : null);
        }

        public static Exception ExceptionToPublish(this Exception e, AmbientFaultContext contextToUse) {
            "[HUB-TRACE][ExceptionToPublish] Entered.".LogToConsole();
            if (contextToUse == null) {
                "[HUB--TRACE][ExceptionToPublish] contextToUse is null, returning original exception.".LogToConsole();
                return e;
            }

            var incomingContextSummary = e is FaultHubException f ? $"'{string.Join(", ", f.Context.CustomContext)}'" : "(none)";
            $"[HUB-TRACE][ExceptionToPublish] Wrapping exception '{e.GetType().Name}'. Incoming Context: {incomingContextSummary}, New Context: '{string.Join(", ", contextToUse.CustomContext)}'".LogToConsole();

            if (e is not FaultHubException faultHubException) {
                "[HUB-TRACE][ExceptionToPublish] Exception is not a FaultHubException. Creating new chain.".LogToConsole();
                return new FaultHubException("An exception occurred in a traced fault context.", e, contextToUse);
            }
            "[HUB-TRACE][ExceptionToPublish] Exception is already a FaultHubException. Chaining new context.".LogToConsole();
            
            // This line is corrected to use the new LogicalStackTrace property.
            var newChainedContext = new AmbientFaultContext { LogicalStackTrace = contextToUse.LogicalStackTrace, CustomContext = contextToUse.CustomContext, InnerContext = faultHubException.Context };
            
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

    public record AmbientFaultContext {
        public IReadOnlyList<LogicalStackFrame> LogicalStackTrace { get; init; }
        public IReadOnlyList<string> CustomContext { get; init; }
        public AmbientFaultContext InnerContext { get; init; }
    }    
    public sealed class FaultHubException : Exception {
        public FaultHubException(string message, Exception innerException, AmbientFaultContext context) 
            : base(message, innerException) {
            Context = context;
            if (innerException == null) return;
            foreach (var key in innerException.Data.Keys) {
                Data[key] = innerException.Data[key];
            }
        }        
        public IEnumerable<LogicalStackFrame> GetLogicalStackTrace() {
            var allStacks = new List<IReadOnlyList<LogicalStackFrame>>();
            var context = Context;
            while (context != null) {
                if (context.LogicalStackTrace != null) {
                    allStacks.Add(context.LogicalStackTrace);
                }
                context = context.InnerContext;
            }
            allStacks.Reverse();
            return allStacks.SelectMany(s => s);
        }
        
        public IEnumerable<string> AllContexts() {
            var context = Context;
            while (context != null) {
                foreach (var s in context.CustomContext) {
                    yield return s;
                }
                context = context.InnerContext;
            }
        }
        public AmbientFaultContext Context { get; }

        public override string ToString() {
            var builder = new StringBuilder();
            builder.AppendLine($"Exception: {GetType().Name}");
            builder.AppendLine($"Message: {Message}");
            builder.AppendLine();
            builder.AppendLine("--- Logical Operation Stack ---");
            var frame = Context;
            var depth = 1;
            AmbientFaultContext innermostFrame = null;
            string lastCaller = null;
            while (frame != null) {
                var currentCaller = frame.CustomContext.FirstOrDefault();
                var specificContext = string.Join(" | ", frame.CustomContext.Skip(1));
                if (currentCaller != lastCaller) {
                    builder.AppendLine($"Operation: {currentCaller}");
                    lastCaller = currentCaller;
                }
                if (!string.IsNullOrEmpty(specificContext)) {
                    builder.AppendLine($"  [Frame {depth++}] Details: '{specificContext}'");
                }
                else {
                    builder.AppendLine($"  [Frame {depth++}]");
                }

                builder.AppendLine("   --- Invocation Stack ---");
                // This section is changed to format the new LogicalStackTrace list.
                if (frame.LogicalStackTrace != null) {
                    var indentedStackTrace = string.Join(Environment.NewLine,
                        frame.LogicalStackTrace.Select(f => $"{f.ToString().TrimStart()}"));
                    builder.AppendLine(indentedStackTrace);
                }

                if (frame.InnerContext == null) {
                    innermostFrame = frame;
                }
                frame = frame.InnerContext;
            }
            builder.AppendLine("--- End of Logical Operation Stack ---");
            builder.AppendLine();
            if (InnerException != null) {
                builder.AppendLine("--- Original Exception ---");
                // This fallback logic for stackless exceptions is also updated.
                if (string.IsNullOrEmpty(InnerException.StackTrace) && innermostFrame?.LogicalStackTrace != null) {
                    builder.AppendLine($"{InnerException.GetType().FullName}: {InnerException.Message}");
                    builder.AppendLine("  --- Stack Trace (from innermost fault context) ---");
                    var indentedInnermost = string.Join(Environment.NewLine,
                        innermostFrame.LogicalStackTrace.Select(f => $"{f.ToString().TrimStart()}"));
                    builder.AppendLine(indentedInnermost);
                }
                else {
                    builder.AppendLine(InnerException.ToString());
                }
                builder.AppendLine("--- End of Original Exception ---");
            }
            return builder.ToString();
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
    public readonly struct LogicalStackFrame(string memberName, string filePath, int lineNumber) {
        public string MemberName => memberName;

        public string FilePath => filePath;

        public int LineNumber => lineNumber;

        public override string ToString() => $"at {memberName} in {filePath}:line {lineNumber}";
    }

}