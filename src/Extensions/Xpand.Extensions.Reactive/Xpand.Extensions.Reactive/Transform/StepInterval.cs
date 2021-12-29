using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Transform{
    public static partial class Transform{
        public static IObservable<T> StepInterval<T>(this IObservable<T> source, TimeSpan minDelay, IScheduler scheduler = null) 
            => source.Select(x => Observable.Empty<T>()
                .Delay(minDelay, scheduler??=DefaultScheduler.Instance)
                .StartWith(x)
            ).Concat();
    }
}