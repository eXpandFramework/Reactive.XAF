using System;
using System.Linq;
using System.Reactive.Linq;
using Xpand.Extensions.Reactive.Combine;

namespace Xpand.Extensions.Reactive.Filter {
    public static partial class Filter {
        public static IObservable<T> DistinctWithPrevious<T, T2>(this IObservable<T[]> source, Func<T, T2> selector)
            => source.CombineWithPrevious().Where(t => t.previous != null)
                .SelectMany(t => t.current.Where(info => {
                    var currentValue = selector(info);
                    return !t.previous.Select(selector).Contains(currentValue);
                }).StartWith(t.previous));
    }
}