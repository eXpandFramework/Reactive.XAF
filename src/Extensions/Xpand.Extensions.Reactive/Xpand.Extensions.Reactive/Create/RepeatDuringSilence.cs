using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Create {
    public static partial class Create {
        public static IObservable<T> RepeatDuringSilence<T>(this IObservable<T> source, TimeSpan maxQuietPeriod,
            Func<T, IObservable<T>> observableSelector, IScheduler scheduler = null) 
            =>  source.Select(x => Observable.Interval(maxQuietPeriod,scheduler??Scheduler.Default)
                .Select(_ => observableSelector(x)).Concat().StartWith(x)).Switch();
    }
}