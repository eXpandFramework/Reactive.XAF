using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using JetBrains.Annotations;
using Xpand.Extensions.Reactive.Combine;

namespace Xpand.Extensions.Reactive.Filter{
    public static partial class Filter{
        [PublicAPI]
        public static IObservable<T> DistinctUntilChanged<T>(this IObservable<T> source, TimeSpan duration,
            IScheduler scheduler = null, Func<T, object> keySelector = null, Func<T, object, bool> matchFunc = null){
            scheduler ??= Scheduler.Default;
            matchFunc ??= (arg1, arg2) => ReferenceEquals(null, arg1) ? ReferenceEquals(null, arg2) : arg1.Equals(arg2);
            keySelector ??= arg => arg;
            var sourcePub = source.Publish().RefCount();
            return sourcePub.GroupByUntil(k => keySelector(k), x => Observable.Timer(duration, scheduler)
                        .TakeUntil(sourcePub.Where(item => !matchFunc(item, x.Key))))
                .SelectMany(y => y.FirstAsync());
        }
    }
}