using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.StringExtensions;
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
            var contextChain = Context.FromHierarchy(c => c.InnerContext).Reverse().ToList();
            var faultChainForStack = contextChain.Select(c => new FaultHubException("", null, c)).ToList();
    
            var rootCause = InnerException;
            while (rootCause is FaultHubException fhEx) {
                rootCause = fhEx.InnerException;
            }
            rootCause ??= InnerException ?? this;
            builder.AppendLine(rootCause.Message);

            var contextFrames = contextChain.Select(ctx => ctx.CustomContext).Where(c => c != null && c.Length != 0);
            var formattedContexts = contextFrames.Select(frame => {
                var strings = frame.Where(o => o != null).Select((o, i) => FormatContextObject(o, i == 0)).Where(s => s != null).ToArray();
                return $"{strings.FirstOrDefault()}{strings.Skip(1).JoinCommaSpace().EncloseParenthesis()}";
            }).ToList();
            var uniqueFormattedContexts = formattedContexts
                .Where((item, index) => index == 0 || item != formattedContexts[index - 1]);
            var contextString = uniqueFormattedContexts.JoinSpace().TrimStart("<- ".ToCharArray());

            if (!string.IsNullOrEmpty(contextString)) {
                builder.AppendLine(contextString);
            }

            var allLogicalFrames = faultChainForStack
                .SelectMany(fhEx => fhEx.LogicalStackTrace ?? [])
                .ToList();
            if (allLogicalFrames.Any()) {
                var fullLogicalStack = allLogicalFrames.Distinct().ToList();
                var simplifiedStack = fullLogicalStack.DistinctBy(f => f.MemberName).ToList();
                var wasSimplified = simplifiedStack.Count < fullLogicalStack.Count;
                if (wasSimplified) {
                    builder.AppendLine("");
                    builder.AppendLine("--- Simplified Invocation Stack (Unique Methods) ---");
                    builder.AppendLine(string.Join(Environment.NewLine, simplifiedStack.Select(f => $"  {f}").Reverse()));
                }

                builder.AppendLine(wasSimplified ? "--- Full Invocation Stack ---" : "--- Invocation Stack ---");
                builder.AppendLine(string.Join(Environment.NewLine, fullLogicalStack.Select(f => $"  {f}").Reverse()));
            }
            
            // builder.AppendLine("--- End of Logical Operation Stack ---");
            
            if (rootCause != this) {
                builder.AppendLine();
                builder.AppendLine("--- Original Exception Details ---");
                builder.AppendLine(rootCause.ToString());
                builder.AppendLine("--- End of Original Exception Details ---");
            }

            return builder.ToString();
        }

    }

    }
