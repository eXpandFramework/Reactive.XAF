using System;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Utility {
    public static partial class Utility {
        public static IObservable<(T Value, TimeSpan Duration)> WithEmissionTime<T>(this IObservable<T> source)
            => source.TimeInterval().Select(i => (i.Value, i.Interval));
    }
}