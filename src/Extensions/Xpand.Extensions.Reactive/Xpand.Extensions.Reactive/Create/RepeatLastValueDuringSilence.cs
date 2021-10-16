using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Create{
    public static partial class Create {
        public static IObservable<T> RepeatLastValueDuringSilence<T>(this IObservable<T> source,
            TimeSpan maxQuietPeriod, IScheduler scheduler = null) {
            scheduler ??= Scheduler.Default;
            return source.Select(x => Observable.Interval(maxQuietPeriod, scheduler).Select(_ => x).StartWith(scheduler, x))
                .Switch();
        }
    }
}