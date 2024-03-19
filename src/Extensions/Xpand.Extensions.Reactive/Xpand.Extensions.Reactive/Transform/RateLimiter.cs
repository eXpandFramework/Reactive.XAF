using System;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Transform {
    public static partial class Transform {
        public static IObservable<T> RateLimiter<T>(this IObservable<T> source, string key, int requestsPerSecond) {
            return Observable.Create<T>(async observer => {
                var rateLimiter = _rateLimiters.GetOrAdd(key, _ => new SemaphoreSlim(requestsPerSecond));

                using (var subscription = source.Subscribe(
                           async item => {
                               await rateLimiter.WaitAsync();
                               try {
                                   observer.OnNext(item);
                               }
                               finally {
                                   rateLimiter.Release();
                               }
                           },
                           observer.OnError,
                           observer.OnCompleted)) {
                    await subscription;
                }

                if (_rateLimiters.TryRemove(key, out var semaphore)) {
                    semaphore.Dispose();
                }
            });
        }
    }
}