using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Transform {
    public static partial class Transform {
        public static IObservable<IDictionary<TKey, TSource>> SampleByKey<TSource, TKey>(this IObservable<TSource> source, 
            Func<TSource, TKey> keySelector, TimeSpan interval, IEqualityComparer<TKey> keyComparer = default) 
            => source.Scan(ImmutableDictionary.Create<TKey, TSource>(keyComparer),
                    (dict, x) => dict.SetItem(keySelector(x), x))
                .Publish(published => Observable.Interval(interval)
                    .WithLatestFrom(published, (_, dict) => dict)
                    .TakeUntil(published.LastOrDefaultAsync()));
    }
}