using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Utility{
    public static partial class Utility{
        public static IObservable<T> DelaySubscription<T>(this IObservable<T> source,
            TimeSpan delay, IScheduler scheduler = null){
            if (scheduler == null) return Observable.Timer(delay).SelectMany(_ => source);
            return Observable.Timer(delay, scheduler).SelectMany(_ => source);
        }
    }
}