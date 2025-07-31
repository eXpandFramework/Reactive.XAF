using System;

namespace Xpand.Extensions.Reactive.ErrorHandling {
    public static partial class ErrorHandling {
        public static IObservable<T> RethrowOnError<T>(this IObservable<T> source, Func<Exception, bool> predicate = null) {
            predicate ??= _ => true;

            // Register a handler that returns the Rethrow action for matching exceptions.
            return source.RegisterHandler(ex => predicate(ex) ? FaultAction.Rethrow : null);
        }
    }
}