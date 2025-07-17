using System;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Xpand.Extensions.Reactive.ErrorHandling;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Reactive.Transform{
    
    public static partial class Transform {
        public static IObservable<TResult> SelectResilient<TSource, TResult>(this IObservable<TSource> source, Func<TSource, TResult> selector)
            => source.SelectMany(x => {
                TResult value;
                try {
                    value = selector(x);          
                }
                catch (Exception ex) {
                    return ex.Tag().Publish<TResult>();  
                }
                return value.Observe();
            });
        
        
        public static IObservable<string> SelectToString(this IObservable<object> source) {
            return source.WhenNotDefault().Select(o => o.ToString());
        }

        [Obsolete(nameof(ExhaustMap),true)]
        public static IObservable<TResult> SelectAndOmit<T, TResult>(this IObservable<T> source,
            Func<T, int, IObservable<TResult>> process, SemaphoreSlim semaphoreSlim = null, Action<T> noProcess = null,
            int maximumConcurrencyCount = 1, [CallerMemberName] string caller = "")
        {
            var dispose = false;
            if (semaphoreSlim == null)
            {
                semaphoreSlim = new SemaphoreSlim(maximumConcurrencyCount, maximumConcurrencyCount);
                dispose = true;
            }

            // Keep track of the number of active operations
            var activeCount = 0;
            var gate = new object();

            var result = source.SelectMany((item, i) =>
            {
                if (semaphoreSlim.Wait(0))
                {
                    // Increment active count
                    lock (gate)
                    {
                        activeCount++;
                    }

                    return process(item, i)
                        .FinallySafe(() =>
                        {
                            semaphoreSlim.Release();

                            // Decrement active count
                            lock (gate)
                            {
                                activeCount--;
                                // If this was the last active operation and we created the semaphore, dispose it
                                if (dispose && activeCount == 0)
                                {
                                    semaphoreSlim.Dispose();
                                }
                            }
                        });
                }

                noProcess?.Invoke(item);
                return Observable.Empty<TResult>();
            });

            return result;
        }

        [Obsolete(nameof(ExhaustMap),true)]
        public static IObservable<TResult> SelectAndOmit<T, TResult>(this IObservable<T> source,
            Func<T, IObservable<TResult>> process,SemaphoreSlim semaphoreSlim=null, Action<T> noProcess=null, int maximumConcurrencyCount = 1,[CallerMemberName]string caller="") 
            => source.SelectAndOmit((item, _) => process(item), semaphoreSlim, noProcess, maximumConcurrencyCount,caller);
    }
}