using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;

namespace Xpand.Extensions.Reactive.Utility {
    public static partial class Utility {
        public static IObservable<T> ObserveOnContext<T>(this IObservable<T> source, SynchronizationContext synchronizationContext)
            => source.ObserveOn(synchronizationContext);
        public static IObservable<T> ObserveOnContext<T>(this IObservable<T> source)
            => source.ObserveOn(SynchronizationContext.Current);
    }
}