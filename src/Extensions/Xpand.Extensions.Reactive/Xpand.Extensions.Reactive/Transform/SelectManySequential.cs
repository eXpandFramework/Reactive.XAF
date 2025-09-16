using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Threading;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;

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
                if (!_semaphores.TryGetValue(key, out var semaphoreRef)) return;
                semaphoreRef.ReferenceCount--;
                if (semaphoreRef.ReferenceCount != 0) return;
                _semaphores.Remove(key);
            }
        }

        public IObservable<TResult> Enqueue<TResult>(TKey key, Func<IObservable<TResult>> action) 
            => Observable.Defer(() => {
                var semaphore = AcquireSemaphore(key);
                return Observable.FromAsync(token => semaphore.WaitAsync(token))
                    .SelectMany(_ => action().Finally(() => semaphore.Release()));
            })
            .Finally(() => ReleaseSemaphore(key));
    }
    public static partial class Transform{
        
        public static IObservable<TResult> SelectManySequential<T1, TResult>(this IObservable<T1> source, Func<T1, IObservable<TResult>> selector) 
            => source.SelectManySequential((arg1, _) => selector(arg1));
        
        public static IObservable<TResult> SelectManySequential<T1, TResult>(this IObservable<T1> source, Func<T1,int, IObservable<TResult>> selector) 
            => source.Select(item => Observable.Defer(() => selector(item,0))).Concat();
        
        
        private static readonly ConditionalWeakTable<object, object> SequencerMap = new();
        
        public static IObservable<TResult> SelectManySequential<TResult, TKey, T>(this T value, Func<IObservable<TResult>> action, Func<T, TKey> keySelector,
            ConcurrentDictionary<TKey, ISubject<Func<IObservable<Unit>>>> queues,[CallerMemberName]string memberName="",[CallerFilePath]string filePath="",[CallerLineNumber]int lineNumber=0) {
            return Observable.Defer(() => {
                var key = keySelector(value);
                return ((AsyncKeyedSequencer<TKey>)SequencerMap.GetValue(queues, _ => new AsyncKeyedSequencer<TKey>()))!
                    .Enqueue(key, action)
                    .PushStackFrame([key], memberName, filePath, lineNumber);
            });
        }
    }
}