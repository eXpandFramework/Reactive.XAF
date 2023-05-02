using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Transform {
    public static partial class Transform {
        /// <summary>
        /// Ignores elements having a specific value, until this value has
        /// been repeated contiguously for a specific duration.
        /// </summary>
        public static IObservable<T> IgnoreNonEstablishedContiguousValue<T>(
            this IObservable<T> source, T value, TimeSpan dueTimeUntilEstablished,
            IEqualityComparer<T> comparer = default, IScheduler scheduler = default) 
            => Observable.Defer(() => {
                IStopwatch stopwatch = null;
                return source.Do(item => {
                        if ((comparer ??= EqualityComparer<T>.Default).Equals(item, value))
                            stopwatch ??= (scheduler ??= global::System.Reactive.Concurrency.Scheduler.Default).StartStopwatch();
                        else
                            stopwatch = null;
                    })
                    .Where(_ => stopwatch == null || stopwatch.Elapsed >= dueTimeUntilEstablished);
            });
    }
}