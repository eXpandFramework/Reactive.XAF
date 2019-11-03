using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Transform{
    public static partial class Transform{
        public static IObservable<T> StepInterval<T>(this IObservable<T> source, TimeSpan minDelay,
            IScheduler scheduler = null){
            scheduler = scheduler ?? Scheduler.Default;
            return source.Select(x => Observable.Empty<T>(scheduler)
                .Delay(minDelay, scheduler)
                .StartWith(scheduler, x)
            ).Concat();
        }
    }
}