using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Xpand.Extensions.Reactive.ErrorHandling;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.Extensions.Reactive.Utility {
    public static partial class Utility {
        public static IObservable<T> DoAlways<T>(this IObservable<T> source, Action always) 
            => source.Publish(obs => obs.DoOnError(_ => always())
                .Merge(obs.DoOnComplete(always)).Merge(obs.Do(_ => always()))
                .IgnoreElements().Merge(obs));
        
        public static SynchronizationContextScheduler Scheduler(this SynchronizationContext context) => new(context);

        public static IObservable<T> DoAfter<T>(this T self,TimeSpan delay,Action execute) 
            => self.Observe().Delay(delay);

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
        public static IObservable<T> DoOnErrorSafe<T>(this IObservable<T> source, Action<Exception> onError) 
            => source.Do(_ => { }, e => {
                try { onError(e);}
                catch (Exception) {
                    // ignored
                }
            });

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

        public static IObservable<T> DoAfterSubscribe<T>(this IObservable<T> source, Action action) 
            => Observable.Create<T>(observer => {
                var disposable = source.Subscribe(observer);
                action();
                return disposable;
            });

        public static IObservable<T> DoOnSubscribe<T>(this IObservable<T> source, Action action) 
            => Observable.Create<T>(observer => {
                action();
                return source.Subscribe(observer);
            });

        public static IObservable<T> DoOnComplete<T>(this IObservable<T> source, Action onComplete)
            => source.Do(_ => { }, onComplete);

        public static IObservable<TSource> DoOnFirst<TSource>(this IObservable<TSource> source, Action<TSource> action)
            => source.DoWhen((i, _) => i == 0, action);
        
        public static IObservable<TSource> DoOnLast<TSource>(this IObservable<TSource> source, Action<TSource> action)
            => source.TakeLast(1).Do( action);
        
        public static IObservable<TSource> DoWhen<TSource>(this IObservable<TSource> source, Func<TSource, bool> predicate, Action<TSource> action,Action<TSource> actionElse=null)
            => source.Do(source1 => {
                if (predicate(source1)) {
                    action(source1);
                }
                else{
                    actionElse?.Invoke(source1);
                }
            });
        
        public static IObservable<TSource> DoWhen<TSource>(this IObservable<TSource> source, Func<int,TSource, bool> predicate, Action<TSource> action)
            => source.Select((source1, i) => {
                if (predicate(i,source1)) {
                    action(source1);
                }
                return source1;
            });

        
        public static IObservable<TSource> Do<TSource>(this IObservable<TSource> source, Action<TSource, int> action)
            => source.Select((source1, i) => {
                action(source1, i);
                return source1;
            });
        
        public static IObservable<TSource> DoWhen<TSource>(this IObservable<TSource> source, Func<int,TSource, bool> predicate, Action<TSource,int> action)
            => source.Select((source1, i) => {
                if (predicate(i,source1)) {
                    action(source1,i);
                }
                return source1;
            });
        
        
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
        
        public static IObservable<T> DoItemResilient<T>(this IObservable<T> source, Action<T> resilientAction, object[] context = null, [CallerMemberName] string caller = "")
            => source.SelectMany(item => Observable.Defer(() => {
                        resilientAction(item);
                        return Observable.Empty<T>();
                    })
                    .ContinueOnError(context, caller)
                    .StartWith(item)
            );

    }
}