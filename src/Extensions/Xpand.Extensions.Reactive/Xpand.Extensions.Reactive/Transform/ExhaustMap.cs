using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Xpand.Extensions.Reactive.Transform{
    public static partial class Transform{
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
    }
}