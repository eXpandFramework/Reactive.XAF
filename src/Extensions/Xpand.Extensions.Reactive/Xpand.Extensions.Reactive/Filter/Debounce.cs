using System;
using System.Collections.Generic;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Filter {
    public static partial class Filter {
        public static IObservable<T> Debounce<T>(this IObservable<T> source, TimeSpan delay, IEqualityComparer<T> comparer = null)
            => source.DistinctUntilChanged(comparer ?? EqualityComparer<T>.Default)
                .Select(x => Observable.Return(x).Delay(delay))
                .Switch();
    }
}