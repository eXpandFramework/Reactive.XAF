using System;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Transform{
    public static partial class Transform{
        public static IObservable<(TSource source, TValue other)> Pair<TSource, TValue>(this IObservable<TSource> source, TValue value) =>
            source.Select(_ => (_, value));
    }
}