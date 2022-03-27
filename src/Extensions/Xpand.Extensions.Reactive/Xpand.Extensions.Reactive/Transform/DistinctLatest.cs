using System;
using System.Collections.Generic;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Transform {
    public static partial class Transform {
        public static IObservable<T> DistinctLatest<T, TKey>(this IObservable<T> newElements,
            IEnumerable<T> seedElements, Func<T, TKey> replacementSelector) 
            => seedElements.ToObservable()
                .Concat(newElements)
                .GroupBy(_ => replacementSelector)
                .SelectMany(grp => grp.Replay(1).Publish().RefCount());
    }
}