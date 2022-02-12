using System;
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
        

    }
}