using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.ErrorHandling{
    public static class ErrorHandling{
        public static IObservable<T> RetryWithBackoff<TException, T>(this IObservable<T> source,
            Func<TException, bool> retryOnError = null, int retryCount = 3,
            Func<int, TimeSpan> strategy = null, IScheduler scheduler = null) where TException : System.Exception{
            strategy ??= (n => TimeSpan.FromSeconds(Math.Pow(n, 2)));
            var attempt = 0;
            retryOnError ??= (_ => true);
            scheduler ??= Scheduler.Default;
            return Observable.Defer(() =>
                    (++attempt == 1 ? source : source.DelaySubscription(strategy(attempt - 1), scheduler))
                    .Select(item => (true, item, (TException) null))
                    .Catch<(bool, T, TException), TException>(e => retryOnError(e)
                        ? Observable.Throw<(bool, T, TException)>(e)
                        : Observable.Return<(bool, T, TException)>((false, default, e))))
                .Retry(retryCount)
                .SelectMany(t => t.Item1
                    ? Observable.Return(t.Item2)
                    : Observable.Throw<T>(t.Item3));
        }
        public static IObservable<T> RetryWithBackoff<T>(this IObservable<T> source,int retryCount = 3,
            Func<int, TimeSpan> strategy = null,Func<System.Exception, bool> retryOnError = null,IScheduler scheduler = null){
            return source.RetryWithBackoff(retryOnError, retryCount, strategy, scheduler);
        }

    }
}