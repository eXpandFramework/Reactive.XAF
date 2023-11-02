using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Reactive.Transform.System {
    public static class TimeSpanExtensions {
        public static IObservable<Unit> After(this TimeSpan timeSpan, Action execute,IScheduler scheduler=null)
            => Unit.Default.Observe().Delay(timeSpan,scheduler??Scheduler.Default)
                .DeferAction(execute).ToUnit();
        public static IObservable<Unit> After(this TimeSpan timeSpan, Action execute,SynchronizationContext context)
            => Unit.Default.Observe().Delay(timeSpan,context.Scheduler()).DeferAction(execute).ToUnit();
        public static IObservable<long> Timer(this TimeSpan dueTime,IScheduler scheduler=null)
            => Observable.Timer(dueTime,scheduler??Scheduler.Default);
        
        public static IObservable<long> Interval(this TimeSpan dueTime)
            => Observable.Interval(dueTime);
    }
}