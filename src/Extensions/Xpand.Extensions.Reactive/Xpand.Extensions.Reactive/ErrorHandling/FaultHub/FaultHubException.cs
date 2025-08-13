using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
}