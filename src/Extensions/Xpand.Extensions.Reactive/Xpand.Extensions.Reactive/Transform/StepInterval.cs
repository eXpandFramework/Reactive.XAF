using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Xpand.Extensions.Reactive.Transform.System;

namespace Xpand.Extensions.Reactive.Transform{
    public static partial class Transform{
        public static IObservable<T> StepInterval<T>(this IObservable<T> source, TimeSpan minDelay, IScheduler scheduler = null) 
            => source.Select(x => minDelay.Timer(scheduler??Scheduler.Default).Select(_ => x)).Concat();
    }
}