using System;
using System.Collections.Concurrent;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Xpand.Extensions.Reactive.ErrorHandling;

namespace Xpand.Extensions.Reactive.Transform{
    public static partial class Transform{
        
        public static IObservable<TResult> SelectManySequential<T1, TResult>(this IObservable<T1> source, Func<T1, IObservable<TResult>> selector, Func<IObservable<TResult>, IObservable<TResult>> retrySelector = null) {
            return source.Select(item => selector(item).ToResilient(retrySelector)).Concat();
        }

        public static IObservable<TResult> SelectManySequential<T1, TResult>(this IObservable<T1> source, Func<T1, Task<TResult>> selector, Func<IObservable<TResult>, IObservable<TResult>> retrySelector = null) 
            => source.SelectManySequential(selector.ToResilient(retrySelector));
        
        public static IObservable<TResult> SelectManySequential<T1, TResult>(this IObservable<T1> source, Func<T1,int, IObservable<TResult>> selector, Func<IObservable<TResult>, IObservable<TResult>> retrySelector = null) 
            => source.Select(selector.ToResilient(retrySelector)).Concat();
        
        public static IObservable<TResult> SelectManySequential<TResult,TKey,T>(this T value, Func<IObservable<TResult>> action, Func<T,TKey> keySelector,
            ConcurrentDictionary<TKey, ISubject<Func<IObservable<Unit>>>> queues, Func<IObservable<TResult>, IObservable<TResult>> retrySelector = null) {
            var key = keySelector(value);
            var subject = queues.GetOrAdd(key, _ => {
                var s = new Subject<Func<IObservable<Unit>>>();
                s.Select(Observable.Defer).Concat().Subscribe();
                return s;
            });

            var tcs = new TaskCompletionSource<TResult>();
            subject.OnNext(() => action.ToResilient(retrySelector)()
                .Do(result => tcs.TrySetResult(result),e => tcs.TrySetException(e),
                    () => { if (!tcs.Task.IsCompleted) tcs.TrySetException(new InvalidOperationException("No result emitted")); })
                .Select(_ => Unit.Default));

            return tcs.Task.ToObservable()
                // .AsResilient()
                ;
        }
    }
}