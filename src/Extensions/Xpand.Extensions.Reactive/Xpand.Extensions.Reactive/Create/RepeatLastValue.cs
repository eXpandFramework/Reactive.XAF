using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Create {
    public static partial class Create {
        public static IObservable<T> RepeatLastValue<T>(this IObservable<T> source,
            Func<T,IObservable<object>> when, IScheduler scheduler = null) {
            scheduler ??= Scheduler.Default;
            return source.Select(x => when(x).Select(_ => x).StartWith(scheduler, x))
                .Switch();
        }

    }
}