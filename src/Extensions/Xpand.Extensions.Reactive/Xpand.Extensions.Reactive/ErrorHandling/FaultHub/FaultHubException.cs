using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xpand.Extensions.LinqExtensions;

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

        public IEnumerable<LogicalStackFrame> LogicalStackTrace 
            => Context.FromHierarchy(frame => frame.InnerContext).Select(frame => frame.LogicalStackTrace)
                .FirstOrDefault(stack => stack != null && stack.Any()) ?? Enumerable.Empty<LogicalStackFrame>();

        public IEnumerable<object> AllContexts 
            => Context.FromHierarchy(context => context.InnerContext)
                .SelectMany(context => context.CustomContext).WhereNotDefault();
        public AmbientFaultContext Context { get; }
        private static string FormatContextObject(object obj) 
            => obj is Type or null ? $"Type: {obj}" :
                obj.ToString() == obj.GetType().FullName ? null :
                $"{(obj.GetType().IsValueType||obj is string ? "Context:" : obj.GetType().Name)} {obj}";

        public override string ToString() {
            var builder = new StringBuilder();
            var exception = InnerException ?? this;
            builder.AppendLine($"{exception.GetType().Name}: {exception.Message}");

            builder.AppendLine("--- Logical Operation Stack ---");
            var frames = Context.FromHierarchy(frame => frame.InnerContext).Reverse().ToList();
            foreach (var frame in frames)
                builder.AppendLine($"{frame.CustomContext.Select(FormatContextObject).WhereNotDefault().Join(" | ")}");

            var fullLogicalStack = LogicalStackTrace.ToList();
            if (fullLogicalStack.Any()) {
                var simplifiedStack = fullLogicalStack.DistinctBy(f => f.MemberName).ToList();
                var wasSimplified = simplifiedStack.Count < fullLogicalStack.Count;

                if (wasSimplified) {
                    builder.AppendLine("--- Simplified Invocation Stack (Unique Methods) ---");
                    builder.AppendLine(string.Join(Environment.NewLine, simplifiedStack.Select(f => $"  {f}").Reverse()));
                }

                builder.AppendLine(wasSimplified ? "--- Full Invocation Stack ---" : "--- Invocation Stack ---");
                builder.AppendLine(string.Join(Environment.NewLine, fullLogicalStack.Select(f => $"  {f}").Reverse()));
            }

            builder.AppendLine("--- End of Logical Operation Stack ---");

            if (InnerException != null) {
                builder.AppendLine();
                builder.AppendLine("--- Original Exception Details ---");
                builder.AppendLine(InnerException.ToString());
                builder.AppendLine("--- End of Original Exception Details ---");
            }

            return builder.ToString();
        }
    }
    
}
