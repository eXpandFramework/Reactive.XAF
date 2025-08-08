using System;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Xpand.Extensions.Reactive.ErrorHandling;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Reactive.Transform{
    
    public static partial class Transform {

                
        public static IObservable<string> SelectToString(this IObservable<object> source) 
            => source.WhenNotDefault().Select(o => o.ToString());

        [Obsolete(nameof(ExhaustMap),true)]
        public static IObservable<TResult> SelectAndOmit<T, TResult>(this IObservable<T> source,
            Func<T, int, IObservable<TResult>> process, SemaphoreSlim semaphoreSlim = null, Action<T> noProcess = null,
            int maximumConcurrencyCount = 1)
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
                            
                            lock (gate)
                            {
                                activeCount--;
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
            => source.SelectAndOmit((item, _) => process(item), semaphoreSlim, noProcess, maximumConcurrencyCount);
        
        public static IObservable<TResult> SelectResilientItem<TSource, TResult>(this IObservable<TSource> source,
            Func<TSource, TResult> selector) 
            => source.SelectMany(item => item.Defer(() => selector(item).Observe()).ContinueOnError());
        public static IObservable<TResult> SelectItemResilient<TSource, TResult>(this IObservable<TSource> source,
            Func<TSource, TResult> resilientSelector, object[] context = null, [CallerMemberName] string caller = "")
            => source.SelectMany(item => item.DeferItemResilient(() => resilientSelector(item).Observe(),context,caller));
    }
}