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
        public static IObservable<long> Timer(this DateTimeOffset dueTime,TimeSpan period,IScheduler scheduler=null)
            => Observable.Timer(dueTime,period,scheduler??Scheduler.Default);
        
        public static IObservable<long> Interval(this TimeSpan dueTime,bool emitNow=false,IScheduler scheduler=null) {
            var interval = Observable.Interval(dueTime,scheduler:scheduler??Scheduler.Default);
            return !emitNow ? interval : interval.StartWith(0);
        }
        public static IObservable<long> AlignedInterval(this TimeSpan period, IScheduler scheduler = null) 
            => Observable.Defer(() => {
                scheduler ??= Scheduler.Default;
                var now   = scheduler.Now;
                var first = now + (period - TimeSpan.FromTicks(now.Ticks % period.Ticks));
                return first.Timer( period, scheduler);
            });

    }
}