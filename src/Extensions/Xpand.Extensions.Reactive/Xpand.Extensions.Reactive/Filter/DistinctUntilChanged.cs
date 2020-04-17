using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using JetBrains.Annotations;

namespace Xpand.Extensions.Reactive.Filter{
    public static partial class Filter{
        [PublicAPI]
        public static IObservable<T> DistinctUntilChanged<T>(this IObservable<T> source, TimeSpan duration,
            IScheduler scheduler = null, Func<T, object> keySelector = null, Func<T, object, bool> matchFunc = null){
            if (scheduler == null) scheduler = Scheduler.Default;
            if (matchFunc == null){
                matchFunc = (arg1, arg2) =>
                    ReferenceEquals(null, arg1) ? ReferenceEquals(null, arg2) : arg1.Equals(arg2);
            }

            if (keySelector == null)
                keySelector = arg => arg;
            var sourcePub = source.Publish().RefCount();
            return sourcePub.GroupByUntil(k => keySelector(k),
                    x => Observable.Timer(duration, scheduler)
                        .TakeUntil(sourcePub.Where(item => !matchFunc(item, x.Key))))
                .SelectMany(y => y.FirstAsync());
        }
    }
}