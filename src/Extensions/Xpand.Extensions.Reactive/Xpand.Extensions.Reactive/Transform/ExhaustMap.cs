using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Xpand.Extensions.Reactive.Transform{
    public static partial class Transform{
        /// <summary>Invokes an asynchronous function for each element of an observable
        /// sequence, ignoring elements that are emitted before the completion of an
        /// asynchronous function of a preceding element.</summary>
        public static IObservable<TResult> ExhaustMap<TSource, TResult>(this IObservable<TSource> source, Func<TSource, Task<TResult>> function){
            return source.Scan(Task.FromResult<TResult>(default),
                    (previousTask, item) => !previousTask.IsCompleted ? previousTask : HideIdentity(function(item)))
                .DistinctUntilChanged()
                .Concat();
            async Task<TResult> HideIdentity(Task<TResult> task) => await task;
        }
        
        /// <summary>Projects each element to an observable sequence, which is merged
        /// in the output observable sequence only if the previous projected observable
        /// sequence has completed.</summary>
        public static IObservable<TResult> ExhaustMap<TSource, TResult>(this IObservable<TSource> source,
            Func<TSource, IObservable<TResult>> function) {
            return Observable.Using(() => new SemaphoreSlim(1, 1),
                semaphore => source.SelectMany(item => ProjectItem(item, semaphore)));
            IObservable<TResult> ProjectItem(TSource item, SemaphoreSlim semaphore) {
                // Attempt to acquire the semaphore immediately. If successful, return
                // a sequence that releases the semaphore when terminated. Otherwise,
                // return immediately an empty sequence.
                return Observable.If(() => semaphore.Wait(0),
                    Observable
                        .Defer(() => function(item))
                        .Finally(() => semaphore.Release())
                );
            }
        }
        /// <summary>Projects each element to an observable sequence, which is merged
        /// in the output observable sequence only if the previous projected observable
        /// sequence has completed.</summary>
        public static IObservable<TResult> ExhaustMap<TSource, TResult>(this IObservable<TSource> source,
            Func<TSource, IObservable<TResult>> function,SemaphoreSlim semaphoreSlim) {
            return source.SelectMany(item => ProjectItem(item, semaphoreSlim));
            IObservable<TResult> ProjectItem(TSource item, SemaphoreSlim semaphore) {
                // Attempt to acquire the semaphore immediately. If successful, return
                // a sequence that releases the semaphore when terminated. Otherwise,
                // return immediately an empty sequence.
                return Observable.If(() => semaphore.Wait(0),
                    Observable
                        .Defer(() => function(item))
                        .Finally(() => semaphore.Release())
                );
            }
        }
        
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