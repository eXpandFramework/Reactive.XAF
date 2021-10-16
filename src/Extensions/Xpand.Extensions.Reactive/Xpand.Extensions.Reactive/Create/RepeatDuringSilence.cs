using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Create {
    public static partial class Create {
        public static IObservable<T> RepeatDuringSilence<T>(this IObservable<T> source, TimeSpan maxQuietPeriod,
            Func<T, IObservable<T>> observableSelector, IScheduler scheduler = null) {
            scheduler ??= Scheduler.Default;
            return source.Select(x =>
                Observable.Interval(maxQuietPeriod, scheduler).SelectMany(_ => observableSelector(x))
                    .StartWith(scheduler, x)).Switch();
        }
    }
}