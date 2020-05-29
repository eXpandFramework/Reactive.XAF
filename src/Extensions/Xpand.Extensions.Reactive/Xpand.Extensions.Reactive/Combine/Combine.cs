using System;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Combine{
    public static partial class Combine{
        public static IObservable<(TSource first, TOther second)> CombineLatest<TSource, TOther>(
            this IObservable<TSource> source, IObservable<TOther> second) =>
            source.CombineLatest(second, (source1, other) => (first: source1, second: other));
    }
}