using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Xpand.Extensions.ExceptionExtensions;
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
        public virtual bool PreserveType =>GetType()!=typeof(FaultHubException);
        public sealed override IDictionary Data => base.Data;
        public IEnumerable<LogicalStackFrame> LogicalStackTrace => this.SelectMany().OfType<FaultHubException>()
            .SelectMany(fault => fault.Context.FromHierarchy(c => c.InnerContext).Reverse())
            .Select(context => context.LogicalStackTrace)
            .Where(stack => stack != null)
            .SelectMany(stack => stack)
            .Distinct();
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
        public virtual string ErrorStatus=> "completed with errors";

        public override string ToString() {
            try {
                return this.Render();
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