using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Xpand.Extensions.Reactive.Filter;

namespace Xpand.Extensions.Reactive.ErrorHandling {
    public static partial class ErrorHandling {
        public static IObservable<T> RetryWhen<T,TException>(this IObservable<T> source) where TException : Exception 
            => source.RetryWhen(obs => obs.OfType<TException>());
        
        public static IObservable<T> RetryWhen<T,TException>(this IObservable<T> source,Func<TException,IObservable<Unit>> signal) where TException : Exception 
            => source.RetryWhen(obs => obs.OfType<TException>());
        
        public static IObservable<T> RetryWhenObjectDisposed<T>(this IObservable<T> source) 
            => source.RetryWhen<T,ObjectDisposedException>();
        
        // public static IObservable<T> RetryWhen<T,TException>(this IObservable<T> source, 
        //     Func<TException,IObservable<object>> retryOnError,IScheduler scheduler = null) where TException:Exception{
        //     var strategy = SecondsBackoffStrategy;
        //     scheduler ??= DefaultScheduler.Instance;
        //     var attempt = 0;
        //     return Observable.Defer(() => (attempt++ == 0 ? source : source.DelaySubscription(strategy(attempt - 1), scheduler))
        //             .Select(Notification.CreateOnNext)
        //             .Catch((TException ex) => retryOnError(ex).Publish(obs => obs.WhenNotDefault().SelectMany(_=>Observable.Return(Notification.CreateOnError<T>(ex)))
        //                 .Merge(obs.WhenDefault().SelectMany(_=>Observable.Throw<Notification<T>>(ex))))))
        //         .Retry().Dematerialize();
        // }

    }
}