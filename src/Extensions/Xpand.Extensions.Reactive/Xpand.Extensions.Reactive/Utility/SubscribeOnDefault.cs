using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Utility {
    public static partial class Utility {
        public static IObservable<T> SubscribeOnDefault<T>(this IObservable<T> source)
            => source.SubscribeOn(DefaultScheduler.Instance);
    }
}