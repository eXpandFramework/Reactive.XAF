using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xpand.Extensions.LinqExtensions;

using static Xpand.Extensions.Reactive.ErrorHandling.FaultHub.FaultHubLogger;

namespace Xpand.Extensions.Reactive.ErrorHandling.FaultHub{
    public sealed class FaultHubException : Exception {
        public FaultHubException(string message, Exception innerException, AmbientFaultContext context) 
            : base(message, innerException) {
            Context = context;
            if (innerException == null) return;
            foreach (var key in innerException.Data.Keys) {
                Data[key] = innerException.Data[key];
            }
        }
        

        public IEnumerable<LogicalStackFrame> LogicalStackTrace {
            get {
                Log(() => "[TOSTRING_DIAG] --- Entering LogicalStackTrace Property ---");
                var contextHierarchy = Context.FromHierarchy(frame => frame.InnerContext).ToList();
                Log(() => $"[TOSTRING_DIAG] Found {contextHierarchy.Count} contexts in the hierarchy.");

                var allLogicalFrames = contextHierarchy
                    .SelectMany(c => c.LogicalStackTrace ?? Enumerable.Empty<LogicalStackFrame>())
                    .ToList();
                Log(() => $"[TOSTRING_DIAG] Aggregated {allLogicalFrames.Count} total logical frames.");

                return allLogicalFrames;
            }
        }

        public IEnumerable<object> AllContexts 
            => Context.FromHierarchy(context => context.InnerContext)
                .SelectMany(context => context.CustomContext).WhereNotDefault();
        public AmbientFaultContext Context { get; }
        private static string FormatContextObject(object obj) 
            => obj is Type or null ? $"Type: {obj}" :
                obj.ToString() == obj.GetType().FullName ? null :
                $"{(obj.GetType().IsValueType||obj is string ? "Context:" : obj.GetType().Name)} {obj}";

// In Xpand.Extensions.Reactive.ErrorHandling.FaultHub.FaultHubException.cs

public override string ToString() {
    Log(() => "[TOSTRING_DIAG] --- Entering ToString ---");
    var builder = new StringBuilder();

    var contextHierarchy = Context.FromHierarchy(c => c.InnerContext).ToList();
    Log(() => $"[TOSTRING_DIAG] Found {contextHierarchy.Count} contexts in the hierarchy.");

    // MODIFICATION: Replaced the problematic while loop with a clearer, more explicit version.
    Exception rootCause = this;
    while (rootCause is FaultHubException faultHubException && faultHubException.InnerException != null) {
        rootCause = faultHubException.InnerException;
    }
    Log(() => $"[TOSTRING_DIAG] Identified root cause: {rootCause.GetType().Name}: {rootCause.Message}");

    builder.AppendLine($"{rootCause.GetType().Name}: {rootCause.Message}");
    builder.AppendLine();

    builder.AppendLine("--- Logical Operation Stack ---");
    var reversedContexts = contextHierarchy.AsEnumerable().Reverse();
    foreach (var context in reversedContexts) {
        var contextString = string.Join(" | ",
            (context.CustomContext ?? Enumerable.Empty<object>()).Where(o => o != null));
        if (!string.IsNullOrEmpty(contextString)) {
            builder.AppendLine($"Context: {contextString}");
        }
    }

    var allLogicalFrames = contextHierarchy
        .SelectMany(c => c.LogicalStackTrace ?? Enumerable.Empty<LogicalStackFrame>())
        .ToList();
    Log(() => $"[TOSTRING_DIAG] Aggregated {allLogicalFrames.Count} total logical frames.");

    if (allLogicalFrames.Any()) {
        var fullLogicalStack = allLogicalFrames.Distinct().ToList();
        var simplifiedStack = fullLogicalStack.DistinctBy(f => f.MemberName).ToList();
        var wasSimplified = simplifiedStack.Count < fullLogicalStack.Count;

        if (wasSimplified) {
            builder.AppendLine("--- Simplified Invocation Stack (Unique Methods) ---");
            builder.AppendLine(
                string.Join(Environment.NewLine, simplifiedStack.Select(f => $"  {f}").Reverse()));
        }

        builder.AppendLine(wasSimplified ? "--- Full Invocation Stack ---" : "--- Invocation Stack ---");
        builder.AppendLine(string.Join(Environment.NewLine, fullLogicalStack.Select(f => $"  {f}").Reverse()));
    }

    builder.AppendLine("--- End of Logical Operation Stack ---");

    if (rootCause != this && rootCause.StackTrace != null) {
        builder.AppendLine();
        builder.AppendLine("--- Original Exception Details ---");
        builder.AppendLine(rootCause.ToString());
        builder.AppendLine("--- End of Original Exception Details ---");
    }

    return builder.ToString();
}
private static (List<FaultHubException> chain, Exception rootCause) GetFlattenedExceptionInfo(Exception ex) {
            var faultChain = new List<FaultHubException>();
            var rootCause = ex;
            
            var queue = new Queue<Exception>();
            queue.Enqueue(ex);

            while (queue.Count > 0) {
                var current = queue.Dequeue();
                if (current is FaultHubException fhEx) {
                    faultChain.Add(fhEx);
                }

                if (current.InnerException != null) {
                    queue.Enqueue(current.InnerException);
                }
                else {
                    rootCause = current;
                }
            }
            
            if (rootCause is FaultHubException finalFault && finalFault.InnerException != null) {
                rootCause = finalFault.InnerException;
            }

            return (faultChain, rootCause);
        }
    }

    }
