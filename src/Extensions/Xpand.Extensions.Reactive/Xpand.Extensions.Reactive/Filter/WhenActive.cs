using System;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Filter {
    public static partial class Filter {
        public static IObservable<T> WhenAlive<T>(this IObservable<T> source)
            => source.IgnoreElements().Concat(Observable.Never<T>());
    }
}