using System;
using System.Collections.Generic;
using System.Linq;

namespace Xpand.Extensions.ExceptionExtensions {
    public static partial class ExceptionExtensions {
        public static IEnumerable<Exception> SelectMany(this Exception exception) {
            if (exception == null) yield break;

            yield return exception;

            if (exception is AggregateException aggregateException) {
                foreach (var inner in aggregateException.InnerExceptions) {
                    foreach (var ex in inner.SelectMany()) {
                        yield return ex;
                    }
                }
            } else if (exception.InnerException != null) {
                foreach (var ex in exception.InnerException.SelectMany()) {
                    yield return ex;
                }
            }
        }

        public static IEnumerable<System.Exception> FailurePath(this System.Exception exception,System.Exception rootCause,Func<System.Exception, bool> exclude=null) {
            var current = rootCause;
            while (true) {
                var parent = exception.Parent(current,exclude);
                if (parent == null) {
                    yield break;
                }
                yield return parent;
                current = parent;
            }        
        }


        public static System.Exception Parent(this System.Exception exception, System.Exception other, Func<System.Exception, bool> exclude=null) {
            var allNodes = exception.SelectMany().Distinct().ToArray();
            var parent = allNodes.FirstOrDefault(p => p.InnerException == other || p is AggregateException ae && ae.InnerExceptions.Contains(other));
            if (exclude != null) {
                while (parent != null && exclude(parent)) {
                    parent = allNodes.FirstOrDefault(p => p.InnerException == parent || p is AggregateException ae && ae.InnerExceptions.Contains(parent));
                }
            }
            return parent;
        }        
    }
}