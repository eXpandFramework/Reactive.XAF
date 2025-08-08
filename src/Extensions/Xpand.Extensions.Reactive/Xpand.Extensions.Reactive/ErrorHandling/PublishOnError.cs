using System;

namespace Xpand.Extensions.Reactive.ErrorHandling {
    public static partial class ErrorHandling {
        public static IObservable<T> CompleteOnError<T>(this IObservable<T> source, Func<Exception, bool> predicate)
            => source.CompleteOnError(_ => { },predicate);
        public static IObservable<T> PublishOnError<T>(this IObservable<T> source, Func<Exception, bool> predicate)
            => source.PublishOnError(_ => {},predicate);
        public static IObservable<T> PublishOnError<T>(this IObservable<T> source, Action<Exception> onError = null,
            Func<Exception, bool> match = null)
            => source.CompleteOnError(mute: false, onError, match);
    }
    
}