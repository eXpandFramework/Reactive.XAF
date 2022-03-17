using System;
using System.Reactive.Linq;
using System.Threading;
using Xpand.Extensions.Reactive.Combine;

namespace Xpand.Extensions.Reactive.Transform{
    public static partial class Transform {
        public static IObservable<TResult> SelectAndOmit<T, TResult>(this IObservable<T> source,
            Func<T, IObservable<TResult>> process,SemaphoreSlim semaphoreSlim=null, Action<T> noProcess=null, int maximumConcurrencyCount = 1){
            semaphoreSlim ??= new SemaphoreSlim(maximumConcurrencyCount, maximumConcurrencyCount);
            return source.SelectMany(item => {
                    if (semaphoreSlim.Wait(0)){
                        return Observable.Return(process(item)
                            .Finally(() => { semaphoreSlim.Release(); }));
                    }

                    noProcess?.Invoke(item);
                    return Observable.Empty<IObservable<TResult>>();
                })
                .Merge(maximumConcurrencyCount);
        }
    }
}