using System;
using System.Reactive.Linq;
using System.Threading;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Reactive.Transform{
    public static partial class Transform {
        public static IObservable<TResult> SelectAndOmit<T, TResult>(this IObservable<T> source,
            Func<T,int, IObservable<TResult>> process, SemaphoreSlim semaphoreSlim = null, Action<T> noProcess = null, int maximumConcurrencyCount = 1) {
            var dispose = false;
            if (semaphoreSlim == null){
                semaphoreSlim = new SemaphoreSlim(maximumConcurrencyCount, maximumConcurrencyCount);
                dispose = true;
            }

            return source.SelectMany((item, i) => {
                    if (semaphoreSlim.Wait(0)){
                        return Observable.Return(process(item,i)
                            .FinallySafe(() => semaphoreSlim.Release()));
                    }

                    noProcess?.Invoke(item);
                    return Observable.Empty<IObservable<TResult>>();

                })
                .Merge(maximumConcurrencyCount)
                .FinallySafe(() => {
                    if (dispose) {
                        semaphoreSlim.Dispose();
                    }
                });
        }

        public static IObservable<TResult> SelectAndOmit<T, TResult>(this IObservable<T> source,
            Func<T, IObservable<TResult>> process,SemaphoreSlim semaphoreSlim=null, Action<T> noProcess=null, int maximumConcurrencyCount = 1) 
            => source.SelectAndOmit((item, _) => process(item), semaphoreSlim, noProcess, maximumConcurrencyCount);
    }
}