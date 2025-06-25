using System;
using System.Collections.Concurrent;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;

namespace Xpand.Extensions.Reactive.Transform{
    public static partial class Transform{
        public static IObservable<T2> SelectManySequential<T1, T2>(this IObservable<T1> source, Func<T1, IObservable<T2>> selector) 
            => source.Select(x => Observable.Defer(() => selector(x))).Concat();
        public static IObservable<T2> SelectManySequential<T1, T2>(this IObservable<T1> source, Func<T1, Task<T2>> selector) 
            => source.SelectManySequential(arg => selector(arg).ToObservable());
        
        public static IObservable<T2> SelectManySequential<T1, T2>(this IObservable<T1> source, Func<T1,int, IObservable<T2>> selector) 
            => source.Select((x,i) => Observable.Defer(() => selector(x,i))).Concat();
        
        
        public static IObservable<TResult> SelectManySequential<TResult,TKey, T>(this T value, Func<IObservable<TResult>> action, Func<T, TKey> keySelector,
            ConcurrentDictionary<TKey, ISubject<Func<IObservable<Unit>>>> queues) {
            var key = keySelector(value);
            var subject = queues.GetOrAdd(key, _ => {
                var subj = new Subject<Func<IObservable<Unit>>>();
                subj.Select(Observable.Defer) 
                    .Concat() 
                    .Subscribe();

                return subj;
            });
            var tcs = new TaskCompletionSource<TResult>();
            subject.OnNext(() => action().Do(
                        x => tcs.TrySetResult(x),
                        ex => tcs.TrySetException(ex),
                        () => { if (!tcs.Task.IsCompleted) tcs.TrySetException(new InvalidOperationException("No result emitted")); }
                    )
                    .Select(_ => Unit.Default)
            );
            return tcs.Task.ToObservable();
        }

    }
}