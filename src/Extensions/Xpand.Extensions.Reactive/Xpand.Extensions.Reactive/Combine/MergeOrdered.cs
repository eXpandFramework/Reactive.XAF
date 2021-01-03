using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Xpand.Extensions.Reactive.Combine {
    public static partial class Combine {
        public static IObservable<T> MergeOrdered<T>(this IObservable<IObservable<T>> source, int maximumConcurrency = Int32.MaxValue) 
            => Observable.Defer(() => {
                var semaphore = new SemaphoreSlim(maximumConcurrency);
                return source.Select(inner => {
                        var published = inner.Replay();
                        _ = semaphore.WaitAsync().ContinueWith(_ => published.Connect(), TaskScheduler.Default);
                        return published.Finally(() => semaphore.Release());
                    })
                    .Concat();
            });
    }
}