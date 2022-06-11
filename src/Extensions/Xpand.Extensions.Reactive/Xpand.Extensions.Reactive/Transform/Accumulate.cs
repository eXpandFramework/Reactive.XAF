using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Transform {
    public static partial class Transform {
        public static IObservable<IReadOnlyList<T>> AccumulateAll<T>(this IObservable<T> next) 
            => next.Scan(ImmutableList<T>.Empty, (a, b) => a.Add(b));

        public static IObservable<IReadOnlyList<int>> AccumulateAllOrdered(this IObservable<int> source) 
            => source.Scan(ImmutableList<int>.Empty, (a, b) => {
                var res = a.BinarySearch(b);
                if (res <= -1)
                    res = ~res;
                return a.Insert(res, b);
            });

        public static IObservable<IReadOnlyList<double>> AccumulateAllOrdered(this IObservable<double> source) 
            => source.Scan(ImmutableList<double>.Empty, (a, b) => {
                var res = a.BinarySearch(b);
                if (res <= -1)
                    res = ~res;
                return a.Insert(res, b);
            });

        public static IObservable<IReadOnlyList<decimal>> AccumulateAllOrdered(this IObservable<decimal> source) 
            => source.Scan(ImmutableList<decimal>.Empty, (a, b) => {
                var res = a.BinarySearch(b);
                if (res <= -1)
                    res = ~res;
                return a.Insert(res, b);
            });
    }
}