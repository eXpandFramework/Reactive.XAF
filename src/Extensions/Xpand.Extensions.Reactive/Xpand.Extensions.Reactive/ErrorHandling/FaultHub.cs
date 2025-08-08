using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Caching.Memory;
using Xpand.Extensions.ExceptionExtensions;
using Xpand.Extensions.MemoryCacheExtensions;

namespace Xpand.Extensions.Reactive.ErrorHandling {
    public static class FaultHub {
        internal static readonly AsyncLocal<List<Func<Exception, FaultAction?>>> HandlersContext = new();
        public static readonly AsyncLocal<StackTrace> OriginStackTrace = new();
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
                // This is the first time the exception is being wrapped.
                // It becomes the innermost frame.
                return new FaultHubException("An exception occurred in a traced fault context.", e, contextToUse);
            }

            // The exception has already been wrapped. We are adding a new, outer frame.
            "[HUB-TRACE][ExceptionToPublish] Exception is already a FaultHubException. Chaining new context.".LogToConsole();
            var newChainedContext = new AmbientFaultContext {
                InvocationStackTrace = contextToUse.InvocationStackTrace,
                CustomContext = contextToUse.CustomContext,
                InnerContext = faultHubException.Context // Link to the existing context chain.
            };

            var newException = new FaultHubException(
                faultHubException.Message, 
                faultHubException.InnerException, // Always preserve the original root exception.
                newChainedContext);
    
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

    public class AmbientFaultContext {
        public StackTrace InvocationStackTrace { get; init; }
        public IReadOnlyList<string> CustomContext { get; init; }
        public AmbientFaultContext InnerContext { get; init; }
         
    }
    
    public sealed class FaultHubException : Exception {
        public FaultHubException(string message, Exception innerException, AmbientFaultContext context) 
            : base(message, innerException) {
            Context = context;
            if (innerException != null) {
                foreach (var key in innerException.Data.Keys) {
                    Data[key] = innerException.Data[key];
                }
            }
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
                
                builder.AppendLine("    --- Invocation Stack ---");
                var indentedStackTrace = string.Join(Environment.NewLine, 
                    frame.InvocationStackTrace.ToString().Split([Environment.NewLine], StringSplitOptions.RemoveEmptyEntries)
                    .Select(line => $"    {line}"));
                builder.AppendLine(indentedStackTrace);
                
                if (frame.InnerContext == null) {
                    innermostFrame = frame;
                }
                frame = frame.InnerContext;
            }
            builder.AppendLine("--- End of Logical Operation Stack ---");
            builder.AppendLine();

            if (InnerException != null) {
                builder.AppendLine("--- Original Exception ---");
                if (string.IsNullOrEmpty(InnerException.StackTrace) && innermostFrame != null) {
                    builder.AppendLine($"{InnerException.GetType().FullName}: {InnerException.Message}");
                    builder.AppendLine("  --- Stack Trace (from innermost fault context) ---");
                    builder.AppendLine(innermostFrame.InvocationStackTrace.ToString());
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


}