using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.ErrorHandling{
    public static class ErrorHandling{
        public static IObservable<T> RetryWithBackoff<T>(this IObservable<T> source,int retryCount = 3,
            Func<int, TimeSpan> strategy = null,Func<System.Exception, bool> retryOnError = null,IScheduler scheduler = null){
            strategy = strategy ?? (n =>TimeSpan.FromSeconds(Math.Pow(n, 2))) ;
            var attempt = 0;
            retryOnError = retryOnError ?? (_ => true);
            return Observable.Defer(() => (++attempt == 1 ? source : source.DelaySubscription(strategy(attempt - 1), scheduler))
                    .Select(item => (true, item, (System.Exception)null))
                    .Catch<(bool, T, System.Exception), System.Exception>(e =>retryOnError(e)? Observable.Throw<(bool, T, System.Exception)>(e)
                        : Observable.Return<(bool, T, System.Exception)>((false, default, e))))
                .Retry(retryCount)
                .SelectMany(t => t.Item1
                    ? Observable.Return(t.Item2)
                    : Observable.Throw<T>(t.Item3));
        }

    }
}