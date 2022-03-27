using System;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Utility {
    public static partial class Utility {
        public static IObservable<TSource> DoWhen<TSource>(this IObservable<TSource> source,
            Func<TSource, bool> predicate, Action<TSource> action)
            => source.Where(predicate).Do(action);
    }
}