using System;
using System.Reactive.Linq;
using System.Threading;

namespace Xpand.Extensions.Reactive.Utility {
    public static partial class Utility {
        public static IObservable<T> ObserveOn<T>(this IObservable<T> source, SynchronizationContext synchronizationContext,Func<bool> state)
            => !state() ? source : source.ObserveOn(synchronizationContext);
    }
}