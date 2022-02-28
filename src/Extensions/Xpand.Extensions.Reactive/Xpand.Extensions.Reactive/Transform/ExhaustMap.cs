using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading;

namespace Xpand.Extensions.Reactive.Transform{
    public static partial class Transform{
        
        /// <summary>Projects each element to an observable sequence, which is merged
        /// in the output observable sequence only if the previous projected observable
        /// sequence has completed.</summary>
        public static IObservable<TResult> ExhaustMap<TSource, TResult>(this IObservable<TSource> source, Func<TSource, IObservable<TResult>> function) 
            => Observable.Defer(() => {
                int mutex = 0;
                return source.SelectMany(item => Interlocked.CompareExchange(ref mutex, 1, 0) == 0
                        ? function(item).Finally(() => Volatile.Write(ref mutex, 0)) : Observable.Empty<TResult>());
            });
        
        /// <summary>Projects each element to an observable sequence, which is merged
        /// in the output observable sequence only if the previous projected observable
        /// sequence has completed.</summary>
        public static IObservable<TResult> ExhaustMap<TSource, TResult>(this IObservable<TSource> source, Func<TSource,int, IObservable<TResult>> function) 
            => Observable.Defer(() => {
                int mutex = 0;
                return source.SelectMany((item, i) => Interlocked.CompareExchange(ref mutex, 1, 0) == 0
                    ? function(item,i).Finally(() => Volatile.Write(ref mutex, 0)) : Observable.Empty<TResult>());
            });
        

        public static IObservable<TResult> ExhaustMapPerKey<TSource, TKey, TResult>(
            this IObservable<TSource> source, Func<TSource, TKey> keySelector,
            Func<TSource, TKey, IObservable<TResult>> function, int maximumConcurrency, IEqualityComparer<TKey> keyComparer = default) 
            => Observable.Using(() => new SemaphoreSlim(maximumConcurrency, maximumConcurrency), globalSemaphore => source
                .GroupBy(keySelector, keyComparer ??= EqualityComparer<TKey>.Default)
                .SelectMany(group => Observable.Using(() => new SemaphoreSlim(1, 1),
                    localSemaphore => @group.SelectMany(item => Observable.If(() => localSemaphore.Wait(0),
                        Observable.If(() => globalSemaphore.Wait(0), Observable.Defer(() => function(item, @group.Key))
                                .Finally(() => globalSemaphore.Release()))
                            .Finally(() => localSemaphore.Release()))))));
    }
}