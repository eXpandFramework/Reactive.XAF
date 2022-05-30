using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.Extensions.Reactive.Filter {
    public static partial class Filter {
        public static IObservable<T> ThrottleN<T>(this IObservable<T> source,int take, TimeSpan delay, IScheduler scheduler = null) 
            => source.Publish(o => o.Take(take)
                .Concat(o.IgnoreElements().TakeUntil(default(T).ReturnObservable().Delay(delay, scheduler ??= Scheduler.Default)))
                .Repeat()
                .TakeUntil(o.IgnoreElements().Concat(Observable.Return(default(T)))));
    }
}