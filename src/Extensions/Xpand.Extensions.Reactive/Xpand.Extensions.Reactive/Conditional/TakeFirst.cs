using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Xpand.Extensions.Reactive.Conditional {
    public static partial class Conditional {
        public static IObservable<T> TakeFirst<T>(this IObservable<T> source, Func<T, bool> predicate)
            => source.Where(predicate).Take(1);

        public static IConnectableObservable<T> TakeAndReplay<T>(this IObservable<T> source, int count)
            => source.Take(count).Replay(count);
        public static IObservable<T> TakeFirst<T>(this IObservable<T> source)
            => source.Take(1);
    }
}