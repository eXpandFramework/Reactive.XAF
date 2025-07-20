using System;
using System.Collections.Generic;
using System.Linq;
using Swordfish.NET.Collections.Auxiliary;
using Xpand.Extensions.LinqExtensions;

namespace Xpand.Extensions.ExceptionExtensions {
    public static partial class ExceptionExtensions {
        public static IEnumerable<Exception> Flatten(this Exception exception) {
            var exceptions = exception.FromHierarchy(exception1 => exception1.InnerException).WhereNotDefault();
            return exception is not AggregateException aggregateException ? exceptions
                : aggregateException.InnerExceptions.SelectRecursive(e
                        => e is AggregateException ae?ae.InnerExceptions:e.FromHierarchy(e1 => e1.InnerException).WhereNotDefault())
                    .Concat(exceptions);
        }
    }
}