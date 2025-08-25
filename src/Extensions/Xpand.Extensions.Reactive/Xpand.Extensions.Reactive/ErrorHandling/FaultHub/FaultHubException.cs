using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Xpand.Extensions.LinqExtensions;

namespace Xpand.Extensions.Reactive.ErrorHandling.FaultHub{
    public class FaultHubException : Exception {
        public FaultHubException(string message, Exception innerException, AmbientFaultContext context)
            : base(message, innerException) {
            Context = context;
            if (innerException == null) return;
            foreach (DictionaryEntry entry in innerException.Data) {
                Data[entry.Key] = entry.Value;
            }
        }
        
        public sealed override IDictionary Data => base.Data;
        public IEnumerable<LogicalStackFrame> LogicalStackTrace => Context.FromHierarchy(c => c.InnerContext)
            .Select(c => c.LogicalStackTrace).LastOrDefault(list => list != null && list.Any()) ?? Enumerable.Empty<LogicalStackFrame>();
        public IEnumerable<object> AllContexts {
            get {
                var contexts = new List<object>();
                var current = Context;
                while (current != null) {
                    if (current.BoundaryName != null) contexts.Add(current.BoundaryName);
                    if (current.UserContext != null) contexts.AddRange(current.UserContext);
                    current = current.InnerContext;
                }

                return contexts;
            }
        }

        public AmbientFaultContext Context { get; }

        public override string ToString() {
            try {
                return this.Parse().Render();
            }
            catch (Exception e) {
                return $"[!!! INTERNAL ToString() CRASH !!!]\n" +
                       $"Crash Type: {e.GetType().FullName}\n" +
                       $"Message: {e.Message}\n" +
                       $"--- Crash StackTrace ---\n{e.StackTrace}\n\n" +
                       $"--- Base Exception Details ---\n{base.ToString()}";
            }
        }
    }
}