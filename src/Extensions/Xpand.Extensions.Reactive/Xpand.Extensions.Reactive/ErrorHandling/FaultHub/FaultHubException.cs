using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.StringExtensions;
using static Xpand.Extensions.Reactive.ErrorHandling.FaultHub.FaultHubLogger;

namespace Xpand.Extensions.Reactive.ErrorHandling.FaultHub{
    public class FaultHubException : Exception {
        public FaultHubException(string message, Exception innerException, AmbientFaultContext context) 
            : base(message, innerException) {
            Context = context;
            if (innerException == null) return;
            foreach (var key in innerException.Data.Keys) {
                Data[key] = innerException.Data[key];
            }
        }

        public sealed override IDictionary Data =>base.Data;


        public IEnumerable<LogicalStackFrame> LogicalStackTrace {
            get {
                Log(() => "[TOSTRING_DIAG] --- Entering LogicalStackTrace Property ---");
                var contextHierarchy = Context.FromHierarchy(frame => frame.InnerContext).ToList();
                Log(() => $"[TOSTRING_DIAG] Context hierarchy contains {contextHierarchy.Count} frames.");

                var stackTraces = contextHierarchy.Select((frame, i) => {
                    Log(() => $"[TOSTRING_DIAG] Inspecting context frame {i} (Name: {frame.Name})");
                    var stack = frame.LogicalStackTrace;
                    if (stack != null && stack.Any()) {
                        Log(() => $"[TOSTRING_DIAG]   - Found stack trace with {stack.Count()} frames. First frame: {stack.First().MemberName}");
                    } else {
                        Log(() => "[TOSTRING_DIAG]   - No stack trace found on this frame.");
                    }
                    return stack;
                }).ToList();

                var selectedStack = stackTraces.LastOrDefault(stack => stack != null && stack.Any());
                
                if (selectedStack != null) {
                    Log(() => $"[TOSTRING_DIAG] --- Selected Innermost Stack Trace. First frame: {selectedStack.First().MemberName} ---");
                } else {
                    Log(() => "[TOSTRING_DIAG] --- No valid stack trace selected. ---");
                }
                
                return selectedStack ?? Enumerable.Empty<LogicalStackFrame>();
            }
        }


        public IEnumerable<object> AllContexts 
            => Context.FromHierarchy(context => context.InnerContext)
                .SelectMany(context => context.CustomContext).WhereNotDefault();
        public AmbientFaultContext Context { get; }
        private static string FormatContextObject(object obj, bool root) 
            => obj is Type or null ? $"Type: {obj}" :
                obj.ToString() == obj.GetType().FullName ? null :
                $"{(obj.GetType().IsValueType||obj is string ? (root?"<-":null) : obj.GetType().Name)} {obj}";

        public override string ToString() {
            var builder = new StringBuilder();
            var rootCause = GetRootCause();

            // Always print the message and logical context of the top-level exception.
            builder.AppendLine(Message);
            var contextString = GetFormattedContexts();
            if (!string.IsNullOrEmpty(contextString)) {
                builder.AppendLine(contextString);
            }

            // MODIFICATION: Search the entire inner exception chain for an AggregateException
            // to ensure the custom formatter is always used for aggregate failures,
            // regardless of how deeply it's nested.
            var topAggregate = FindTopAggregate(this);

            if (topAggregate != null) {
                FormatAggregateException(builder, topAggregate);
            }
            else {
                FormatSingleException(builder, rootCause);
            }

            return builder.ToString();
        }

        private AggregateException FindTopAggregate(Exception ex) {
            var current = ex;
            while (current != null) {
                if (current is AggregateException agg) return agg;
                current = current.InnerException;
            }
            return null;
        }
        private Exception GetRootCause() {
            var current = InnerException;
            while (current is FaultHubException fhEx) {
                current = fhEx.InnerException;
            }
            return current ?? InnerException ?? this;
        }

        private string GetFormattedContexts() {
            var contextChain = Context.FromHierarchy(c => c.InnerContext).Reverse().ToList();
            var contextFrames = contextChain.Select(ctx => ctx.CustomContext).Where(c => c != null && c.Any());
            var formattedContexts = contextFrames.Select(frame => {
                var strings = frame.Where(o => o != null).Select((o, i) => FormatContextObject(o, i == 0)).Where(s => s != null).ToArray();
                return $"{strings.FirstOrDefault()}{strings.Skip(1).JoinCommaSpace().EncloseParenthesis()}";
            });
            var uniqueFormattedContexts = formattedContexts
                .Where((item, index) => index == 0 || item != formattedContexts.ElementAt(index - 1));
            return uniqueFormattedContexts.JoinSpace().TrimStart("<- ".ToCharArray());
        }

        private void FormatSingleException(StringBuilder builder, Exception rootCause) {
            var allLogicalFrames = Context.FromHierarchy(c => c.InnerContext)
                .SelectMany(context => context.LogicalStackTrace ?? Enumerable.Empty<LogicalStackFrame>())
                .ToList();
            
            if (allLogicalFrames.Any()) {
                builder.AppendLine();
                builder.AppendLine("--- Invocation Stack ---");
                builder.AppendLine(string.Join(Environment.NewLine, allLogicalFrames.Distinct().Select(f => $"  {f}")));
            }

            if (rootCause != this && rootCause != null) {
                builder.AppendLine();
                builder.AppendLine("--- Original Exception Details ---");
                builder.AppendLine(rootCause.ToString());
                builder.AppendLine("--- End of Original Exception Details ---");
            }
        }

        private void FormatAggregateException(StringBuilder builder, AggregateException aggregate) {
            var rootCauses = FindRootCauses(aggregate).ToList();
            builder.AppendLine();
            builder.AppendLine($"--- Root Causes ({rootCauses.Count}) ---");
            for (var i = 0; i < rootCauses.Count; i++) {
                builder.AppendLine($"[{i + 1}] {rootCauses[i].GetType().Name}: {rootCauses[i].Message}");
            }

            builder.AppendLine();
            builder.AppendLine("--- Detailed Failure Paths ---");
            var flatList = aggregate.Flatten().InnerExceptions;
            for (var i = 0; i < flatList.Count; i++) {
                BuildFailureTree(builder, flatList[i], "", i == flatList.Count - 1);
            }
        }

        private static IEnumerable<Exception> FindRootCauses(Exception ex) {
// MODIFICATION: The logic is now enhanced to recursively look inside FaultHubException wrappers,
// in addition to AggregateExceptions. This ensures it finds the true underlying exceptions
// instead of stopping at our framework's summary exceptions.
            if (ex is AggregateException aggEx) {
                foreach (var inner in aggEx.InnerExceptions) {
                    foreach (var root in FindRootCauses(inner)) {
                        yield return root;
                    }
                }
            }
            else if (ex is FaultHubException fhEx && fhEx.InnerException != null) {
                foreach (var root in FindRootCauses(fhEx.InnerException)) {
                    yield return root;
                }
            }
            else {
                yield return ex;
            }
        }
        
        private void BuildFailureTree(StringBuilder builder, Exception ex, string indent, bool isLast) {
            var prefix = indent + (isLast ? "└─" : "├─");
            var childIndent = indent + (isLast ? "   " : "│  ");

            var faultHubException = ex as FaultHubException ?? new FaultHubException("Unrecognized Exception", ex, Context);
            
            builder.AppendLine($"{prefix} [FAIL] {faultHubException.GetFormattedContexts()}");
            
            var allLogicalFrames = faultHubException.Context.FromHierarchy(c => c.InnerContext)
                .SelectMany(context => context.LogicalStackTrace ?? Enumerable.Empty<LogicalStackFrame>())
                .ToList();

            if (allLogicalFrames.Any()) {
                builder.AppendLine($"{childIndent} --- Invocation Stack ---");
                foreach (var frame in allLogicalFrames.Distinct()) {
                    builder.AppendLine($"{childIndent}   {frame}");
                }
            }

            var rootCause = faultHubException.GetRootCause();
            if (rootCause is AggregateException aggEx) {
                var innerExceptions = aggEx.Flatten().InnerExceptions;
                for (var i = 0; i < innerExceptions.Count; i++) {
                    BuildFailureTree(builder, innerExceptions[i], childIndent, i == innerExceptions.Count - 1);
                }
            } else if (rootCause != null) {
                builder.AppendLine($"{childIndent} └─ Root Cause: {rootCause.GetType().Name}: {rootCause.Message}");
            }
        }
    }
    }

    
