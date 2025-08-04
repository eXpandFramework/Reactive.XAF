using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xpand.Extensions.Reactive.ErrorHandling;

namespace Xpand.Extensions.Reactive.Transform{
    internal class AsyncKeyedSequencer<TKey> {
        private class SemaphoreReference {
            public SemaphoreSlim Semaphore { get; } = new(1, 1);
            public int ReferenceCount { get; set; }
        }

        private readonly Dictionary<TKey, SemaphoreReference> _semaphores = new();
        private readonly Lock _lock = new();

        private SemaphoreSlim AcquireSemaphore(TKey key) {
            lock (_lock) {
                if (!_semaphores.TryGetValue(key, out var semaphoreRef)) {
                    semaphoreRef = new SemaphoreReference();
                    _semaphores[key] = semaphoreRef;
                }

                semaphoreRef.ReferenceCount++;
                return semaphoreRef.Semaphore;
            }
        }

        private void ReleaseSemaphore(TKey key) {
            lock (_lock) {
                if (_semaphores.TryGetValue(key, out var semaphoreRef)) {
                    semaphoreRef.ReferenceCount--;
                    if (semaphoreRef.ReferenceCount == 0)
                        // If idle, remove the semaphore to prevent memory leaks.
                        _semaphores.Remove(key);
                }
            }
        }

        public IObservable<TResult> Enqueue<TResult>(TKey key, Func<IObservable<TResult>> action) {
            // Use Observable.FromAsync to integrate async/await patterns robustly with Rx.
            return Observable.FromAsync(async cancellationToken => {
                var semaphore = AcquireSemaphore(key);

                await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                try {
                    // Execute the action and wait for it to complete.
                    return await action().ToTask(cancellationToken).ConfigureAwait(false);
                }
                finally {
                    // CRITICAL: Guaranteed release even if the action fails or is cancelled.
                    semaphore.Release();
                    ReleaseSemaphore(key);
                }
            });
        }
    }
    public static partial class Transform{
        
        public static IObservable<TResult> SelectManySequential<T1, TResult>(this IObservable<T1> source, Func<T1, IObservable<TResult>> selector,[CallerMemberName]string caller="") 
            => source.SelectManySequential((arg1, _) => selector(arg1),caller);
        
        public static IObservable<TResult> SelectManySequential<T1, TResult>(this IObservable<T1> source, Func<T1,int, IObservable<TResult>> selector,[CallerMemberName]string caller="") 
            => source.Select(item => Observable.Defer(() => selector(item,0)).ChainFaultContext([item], caller: caller)).Concat();
        
        // Maps the user's dictionary instance (the handle) to the robust sequencer instance.
        // We use <object, object> because ConditionalWeakTable requires reference types, 
        // and we need to store the generic AsyncKeyedSequencer<TKey>.
        private static readonly ConditionalWeakTable<object, object> SequencerMap = new();

        /// <summary>
        /// Ensures that actions are executed sequentially based on a key.
        /// This implementation is robust against errors and prevents memory leaks,
        /// while maintaining signature compatibility with the original version.
        /// </summary>
        public static IObservable<TResult> SelectManySequential<TResult, TKey, T>(
            this T value,
            Func<IObservable<TResult>> action,
            Func<T, TKey> keySelector,
            // This parameter is kept for compatibility but is only used as an identity handle.
            ConcurrentDictionary<TKey, ISubject<Func<IObservable<Unit>>>> queues)
        {
            if (queues == null) throw new ArgumentNullException(nameof(queues));

            // Look up or create the robust sequencer associated with the specific dictionary 
            // instance provided by the caller.
            var sequencer = (AsyncKeyedSequencer<TKey>)SequencerMap.GetValue(
                queues,
                // Factory function to create a new sequencer if one doesn't exist for this dictionary.
                _ => new AsyncKeyedSequencer<TKey>()
            );

            var key = keySelector(value);

            // Delegate the work to the robust implementation.
            return sequencer.Enqueue(key, action);
        }
        
        // public static IObservable<TResult> SelectManySequential<TResult,TKey,T>(this T value, Func<IObservable<TResult>> action, Func<T,TKey> keySelector,
        //     ConcurrentDictionary<TKey, ISubject<Func<IObservable<Unit>>>> queues) {
        //     var key = keySelector(value);
        //     var subject = queues.GetOrAdd(key, _ => {
        //         var s = new Subject<Func<IObservable<Unit>>>();
        //         s.Select(Observable.Defer).Concat().Subscribe();
        //         return s;
        //     });
        //
        //     var tcs = new TaskCompletionSource<TResult>();
        //     subject.OnNext(() => action()
        //         .Do(result => tcs.TrySetResult(result),e => tcs.TrySetException(e),
        //             () => { if (!tcs.Task.IsCompleted) tcs.TrySetException(new InvalidOperationException("No result emitted")); })
        //         .Select(_ => Unit.Default));
        //
        //     return tcs.Task.ToObservable();
        // }
    }
}