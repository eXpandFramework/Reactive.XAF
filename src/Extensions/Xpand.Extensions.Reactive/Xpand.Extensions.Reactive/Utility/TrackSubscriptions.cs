using System;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Utility {
    public static partial class Utility {
        public static IObservable<T> TrackSubscriptions<T>(this IObservable<T> source, SubscriptionCounter counter) {
            return Observable.Defer(() => {
                counter.Increment();
                return source;
            });
        }
    }
    public sealed class SubscriptionCounter {
        public int Count { get; private set; }
        public void Increment() => Count++;
        public override string ToString() => Count.ToString();
    }

}