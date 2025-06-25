using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Xpand.Extensions.Numeric;
using Xpand.Extensions.Reactive.Transform.System;

namespace Xpand.Extensions.Reactive.Transform{
    public static partial class Transform{
        public static IObservable<T> StepInterval<T>(this IObservable<T> source, TimeSpan minDelay, IScheduler scheduler = null) 
            => source.Select(x => minDelay.Timer(scheduler??Scheduler.Default).Select(_ => x)).Concat();

        public static IObservable<T> StepRandomInterval<T>(this IObservable<T> source, double minDelaySeconds, double maxDelaySeconds, IScheduler scheduler = null)
            => source.StepRandomInterval(minDelaySeconds.Seconds(), maxDelaySeconds.Seconds(), scheduler);

        public static IObservable<T> StepRandomInterval<T>(this IObservable<T> source, TimeSpan minDelay, TimeSpan maxDelay, IScheduler scheduler=null) 
            => source.StepRandomInterval(new Random(), minDelay, maxDelay, scheduler);

        public static IObservable<T> StepRandomInterval<T>(this IObservable<T> source,Random random, TimeSpan minDelay, TimeSpan maxDelay, IScheduler scheduler=null) 
            => source.Select(x => random.Next((int)minDelay.TotalMilliseconds, (int)maxDelay.TotalMilliseconds)
                .Milliseconds().Timer(scheduler ?? Scheduler.Default).Select(_ => x)).Concat();
    }
}