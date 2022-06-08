using System;
using System.Collections.Generic;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Transform {
    public static partial class Transform {
        public static IObservable<IList<T>> BufferUntilInactive<T>(this IObservable<T> source, TimeSpan delay)
            => source.Publish(obs => obs.Window(() => obs.Throttle(delay)).SelectMany(window => window.ToList()));
    }
}