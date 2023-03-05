using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.Extensions.Reactive.Utility {
    public static partial class Utility {
        public static SynchronizationContextScheduler Scheduler(this SynchronizationContext context) => new(context);
        
        public static IObservable<T> Do<T>(this T self,Action execute) {
            execute();
            return self.ReturnObservable();
        }

        

        public static IObservable<T> DoAfter<T>(this T self,TimeSpan delay,Action execute) 
            => self.ReturnObservable().Delay(delay);

        public static IObservable<T> TryDo<T>(this IObservable<T> source, Action<T> tryDo)
            => source.Do(obj => {
                try {
                    tryDo(obj);
                }
                catch {
                    // ignored
                }
            });
        
        public static IObservable<T> DoOnError<T>(this IObservable<T> source, Action<Exception> onError) 
            => source.Do(_ => { }, onError);

        public static IObservable<T> DoSafe<T>(this IObservable<T> source, Action<T> action)
            => source.Do(obj => {
                try {
                    action(obj);
                }
                catch (Exception) {
                    // ignored
                }
            });
        
        public static IObservable<T> DoOnPrevious<T>(this IObservable<T> source, Action<T> onPrevious)
            => source.Select(x => (Item: x, HasValue: true))
                .Append((default, false))
                .Scan((previous, current) => {
                    if (previous.HasValue) onPrevious(previous.Item);
                    return current;
                })
                .Where(entry => entry.HasValue)
                .Select(entry => entry.Item);

        public static IObservable<T> DoOnSubscribe<T>(this IObservable<T> source, Action action) 
            => Observable.Defer(() => {
                action();
                return source;
            });

        public static IObservable<T> DoOnComplete<T>(this IObservable<T> source, Action onComplete)
            => source.Do(_ => { }, onComplete);

        public static IObservable<TSource> DoOnFirst<TSource>(this IObservable<TSource> source, Action<TSource> action)
            => source.DoWhen((i, _) => i == 0, action);
        
        public static IObservable<TSource> DoOnLast<TSource>(this IObservable<TSource> source, Action<TSource> action)
            => source.TakeLast(1).Do( action);
        
        public static IObservable<TSource> DoWhen<TSource>(this IObservable<TSource> source, Func<TSource, bool> predicate, Action<TSource> action)
            => source.Do(source1 => {
                if (predicate(source1)) {
                    action(source1);
                }
            });
        public static IObservable<TSource> DoWhen<TSource>(this IObservable<TSource> source, Func<int,TSource, bool> predicate, Action<TSource> action)
            => source.Select((source1, i) => {
                if (predicate(i,source1)) {
                    action(source1);
                }
                return source1;
            });
        public static IObservable<TSource> DoWhen<TSource>(this IObservable<TSource> source, Func<int,TSource, bool> predicate, Action<TSource,int> action)
            => source.Select((source1, i) => {
                if (predicate(i,source1)) {
                    action(source1,i);
                }
                return source1;
            });
        
        /// <summary>
        /// Invokes an action sequentially for each element in the observable sequence,
        /// on the specified scheduler, skipping and dropping elements that are received
        /// during the execution of a previous action, except from the latest element.
        /// </summary>
        public static IObservable<TSource> DroppingDo<TSource>(this IObservable<TSource> source, Action<TSource> action, IScheduler scheduler = null) 
            => Observable.Defer(() => {
                Tuple<TSource> latest = null;
                return source.Select(item => {
                        var previous = Interlocked.Exchange(ref latest, Tuple.Create(item));
                        if (previous != null) return Observable.Empty<TSource>();
                        return Observable.Defer(() => {
                            var current = Interlocked.Exchange(ref latest, null);
                            var unBoxed = current.Item1;
                            return Observable.Start(() => {
                                action(unBoxed);
                                return unBoxed;
                            }, scheduler ??= System.Reactive.Concurrency.Scheduler.Default);
                        });
                    })
                    .Concat();
            });

    }
}