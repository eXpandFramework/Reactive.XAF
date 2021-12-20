using System;
using System.Reactive.Linq;
using System.Threading;

namespace Xpand.Extensions.Reactive.Transform{
    public static partial class Transform{
        public static IObservable<TResult> SelectAndOmit<T, TResult>(this IObservable<T> source,
            Func<T, IObservable<TResult>> process, Action<T> noProcess, int maximumConcurrencyCount = 1){
            var semaphore = new SemaphoreSlim(maximumConcurrencyCount, maximumConcurrencyCount);
            return source.SelectMany(item => {
                    if (semaphore.Wait(0)){
                        return Observable.Return(process(item).Finally(() => { semaphore.Release(); }));
                    }

                    noProcess(item);
                    return Observable.Empty<IObservable<TResult>>();
                })
                .Merge(maximumConcurrencyCount);
        }
    }
}