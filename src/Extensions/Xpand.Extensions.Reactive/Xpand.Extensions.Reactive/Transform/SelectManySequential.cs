using System;
using System.Collections.Concurrent;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Xpand.Extensions.Reactive.ErrorHandling;

namespace Xpand.Extensions.Reactive.Transform{
    public static partial class Transform{
        public static IObservable<T2> SelectManySequential<T1, T2>(this IObservable<T1> source, Func<T1, IObservable<T2>> selector) 
            => source.Select(selector.WithResilience()).Concat();
        public static IObservable<T2> SelectManySequential<T1, T2>(this IObservable<T1> source, Func<T1, Task<T2>> selector) 
            => source.SelectManySequential(selector.WithResilience());
        
        public static IObservable<T2> SelectManySequential<T1, T2>(this IObservable<T1> source, Func<T1,int, IObservable<T2>> selector) 
            => source.Select(selector.WithResilience()).Concat();
        
        public static IObservable<TResult> SelectManySequential<TResult,TKey,T>(this T value,
            Func<IObservable<TResult>> action, Func<T,TKey> keySelector,
            ConcurrentDictionary<TKey, ISubject<Func<IObservable<Unit>>>> queues) {
            var key = keySelector(value);
            var subject = queues.GetOrAdd(key, _ => {
                var s = new Subject<Func<IObservable<Unit>>>();
                s.Select(Observable.Defer).Concat().Subscribe();
                return s;
            });

            var tcs = new TaskCompletionSource<TResult>();
            subject.OnNext(() => action.WithResilience()()
                .Do(result => tcs.TrySetResult(result),e => tcs.TrySetException(e),
                    () => { if (!tcs.Task.IsCompleted) tcs.TrySetException(new InvalidOperationException("No result emitted")); })
                .Select(_ => Unit.Default));

            return tcs.Task.ToObservable();
        }
    }
}