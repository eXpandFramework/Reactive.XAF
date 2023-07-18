using System;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Conditional {
    public static partial class Conditional {
        public static IObservable<T> TakeFirst<T>(this IObservable<T> source, Func<T, bool> predicate)
            => source.Where(predicate).Take(1);
        public static IObservable<T> TakeFirst<T>(this IObservable<T> source)
            => source.Take(1);
    }
}