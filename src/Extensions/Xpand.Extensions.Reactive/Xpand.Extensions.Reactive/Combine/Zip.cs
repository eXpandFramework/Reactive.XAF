using System;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Combine{
    public static partial class Combine{
        public static IObservable<(TSource first, TOther second)> Zip<TSource, TOther>(
            this IObservable<TSource> source, IObservable<TOther> second){
            return source.Zip(second, (source1, other) => (first: source1, second: other));
        }
    }
}