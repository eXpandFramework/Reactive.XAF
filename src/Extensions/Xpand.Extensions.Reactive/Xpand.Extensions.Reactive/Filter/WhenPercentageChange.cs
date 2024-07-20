using System;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Filter {
    public static partial class Filter {
        public static IObservable<decimal> WhenPercentageChange(this IObservable<decimal> source, decimal percentageThreshold) 
            => source.Scan((lastEmitted: (decimal?)null, toEmit: (decimal?)null), (acc, current) => acc.lastEmitted == null
                ? (current, current) : Math.Abs((current - acc.lastEmitted.Value) / acc.lastEmitted.Value) > percentageThreshold
                    ? (current, current) : (acc.lastEmitted, null)).Where(result => result.toEmit.HasValue)
                .Select(result => result.toEmit!.Value);
    }
}