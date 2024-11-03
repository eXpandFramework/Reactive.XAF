using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Xpand.Extensions.Fasterflect;
using Xpand.Extensions.Reactive.Transform.System;
using Xpand.Extensions.TypeExtensions;

namespace Xpand.Extensions.Reactive.Transform {
    public static partial class Transform {
        public static IObservable<T> RepeatDefaultValueDuringSilence<T>(this IObservable<T> source,
            TimeSpan maxQuietPeriod, IScheduler scheduler = null)
            => source.RepeatDuringSilence(maxQuietPeriod, _ => typeof(T).DefaultValue().Observe().Cast<T>(),scheduler);

        public static IObservable<T> RepeatNewInstanceDuringSilence<T>(this IObservable<T> source, TimeSpan maxQuietPeriod, IScheduler scheduler = null) 
            =>  source.RepeatDuringSilence(maxQuietPeriod, _ => typeof(T).CreateInstance<T>().Observe(),scheduler);
        public static IObservable<T> RepeatDuringSilence<T>(this IObservable<T> source, TimeSpan maxQuietPeriod,
            Func<T, IObservable<T>> observableSelector, IScheduler scheduler = null) 
            =>  source.Select(x => Observable.Interval(maxQuietPeriod,scheduler??Scheduler.Default)
                .Select(_ => observableSelector(x)).Concat().StartWith(x)).Switch();
        public static IObservable<T[]> RepeatEmptyDuringSilence<T>(this IObservable<T[]> source, TimeSpan maxQuietPeriod, IScheduler scheduler = null)
            => source.RepeatDuringSilence(maxQuietPeriod, _ => Array.Empty<T>().Observe());
        public static IObservable<T> RepeatLastValue<T>(this IObservable<T> source,
            Func<T,IObservable<object>> when, IScheduler scheduler = null) {
            scheduler ??= Scheduler.Default;
            return source.Select(x => when(x).Select(_ => x).StartWith(scheduler, x))
                .Switch();
        }

        public static IObservable<T> RepeatLastValueDuringSilence<T>(this IObservable<T> source,
            TimeSpan maxQuietPeriod, IScheduler scheduler = null) {
            scheduler ??= Scheduler.Default;
            return source.Select(x => Observable.Interval(maxQuietPeriod, scheduler).Select(_ => x).StartWith(scheduler, x))
                .Switch();
        }

        public static IObservable<TSource> RepeatUntilCompleted<TSource, TOther>(this IObservable<TSource> source, IObservable<TOther> other) 
            => Observable.Create<TSource>(observer => {
                var disposables = new CompositeDisposable();
                var completed = false;
                disposables.Add((other ??= (IObservable<TOther>)source).Subscribe(_ => { }, () => completed = true));
                disposables.Add(Observable.While(() => !completed, source).Subscribe(observer));
                return disposables;
            });

        public static IObservable<T> RepeatWhen<T>(this IObservable<T> source, TimeSpan timeSpan)
            => source.RepeatWhen(obs => obs.SelectMany(_ => timeSpan.Timer()));
        
        public static IObservable<T> RepeatUntilEmpty<T>(this IObservable<T> source) 
            => source.Materialize()
                .Repeat()
                .StartWith((Notification<T>)null)
                .Buffer(2, 1)
                .Select(it => {
                    if (it[1].Kind != NotificationKind.OnCompleted) {
                        return it[1];
                    }
                    // it[1] is OnCompleted, check the previous one
                    if (it[0] != null && it[0].Kind != NotificationKind.OnCompleted) {
                        // not a consecutive OnCompleted, so we ignore this OnCompleted with a NULL marker
                        return null;
                    }

                    // okay, we have two consecutive OnCompleted, stop this observable.
                    return it[1];
                })
                .Where(it => it != null) // remove the NULL marker
                .Dematerialize();
    }
}