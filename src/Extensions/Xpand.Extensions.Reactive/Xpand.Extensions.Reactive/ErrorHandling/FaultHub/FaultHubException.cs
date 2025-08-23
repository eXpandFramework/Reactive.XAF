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
        public IEnumerable<object> AllContexts => Context.FromHierarchy(c => c.InnerContext).SelectMany(c => c.UserContext).WhereNotDefault();
        public AmbientFaultContext Context { get; }

        public override string ToString() {
            try {
                return this.Parse().Render();
            }
            catch (Exception e) {
                return $"[FaultHub Report Generation Failed: {e.Message}] \n--- Base Exception Details ---\n{base.ToString()}";
            }
        }


        
        
        
    }

    
}