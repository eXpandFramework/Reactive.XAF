using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Utility {
    public static partial class Utility {
        public static IObservable<T> ObserveOnDefault<T>(this IObservable<T> source)
            => source.ObserveOn(DefaultScheduler.Instance);
    }
}