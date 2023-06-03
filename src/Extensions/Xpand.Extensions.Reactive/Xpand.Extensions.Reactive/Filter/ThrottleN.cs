using System;
using System.Diagnostics;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.Extensions.Reactive.Filter {
    public static partial class Filter {
        public static IObservable<T> WhenDebuggerThrottle<T>(this IObservable<T> source, TimeSpan delay) {
            if (Debugger.IsAttached) {
                return source.Throttle(delay);
            }

            return source;
        }

        public static IObservable<T> ThrottleN<T>(this IObservable<T> source,int take, TimeSpan delay, IScheduler scheduler = null) 
            => source.Publish(o => o.Take(take)
                .Concat(o.IgnoreElements().TakeUntil(default(T).Observe().Delay(delay, scheduler ??= Scheduler.Default)))
                .Repeat()
                .TakeUntil(o.IgnoreElements().Concat(Observable.Return(default(T)))));
    }
}