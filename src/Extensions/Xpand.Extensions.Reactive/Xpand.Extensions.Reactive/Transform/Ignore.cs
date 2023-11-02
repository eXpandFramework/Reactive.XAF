using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Transform {
    public static partial class Transform {
        
        public static IObservable<T> IgnoreNonEstablishedContiguousValue<T>(
            this IObservable<T> source, T value, TimeSpan dueTimeUntilEstablished,
            IEqualityComparer<T> comparer = default, IScheduler scheduler = default) 
            => Observable.Defer(() => {
                IStopwatch stopwatch = null;
                return source.Do(item => {
                        if ((comparer ??= EqualityComparer<T>.Default).Equals(item, value))
                            stopwatch ??= (scheduler ??= Scheduler.Default).StartStopwatch();
                        else
                            stopwatch = null;
                    })
                    .Where(_ => stopwatch == null || stopwatch.Elapsed >= dueTimeUntilEstablished);
            });
    }
}