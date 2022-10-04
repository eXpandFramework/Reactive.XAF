using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Reactive.Transform.System {
    public static class TimeSpanExtensions {
        public static IObservable<Unit> After(this TimeSpan timeSpan, Action execute,IScheduler scheduler=null)
            => Unit.Default.ReturnObservable().Delay(timeSpan,scheduler??Scheduler.Default)
                .Do(execute).ToUnit();
        public static IObservable<Unit> After(this TimeSpan timeSpan, Action execute,SynchronizationContext context)
            => Unit.Default.ReturnObservable().Delay(timeSpan,context.Scheduler()).Do(execute).ToUnit();
        public static IObservable<long> Timer(this TimeSpan dueTime)
            => Observable.Timer(dueTime);
    }
}