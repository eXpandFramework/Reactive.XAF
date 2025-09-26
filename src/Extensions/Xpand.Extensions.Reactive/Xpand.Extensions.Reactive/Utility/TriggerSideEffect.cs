using System;
using System.Reactive.Linq;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.Extensions.Reactive.Utility {
    public static partial class Utility {
        public static IObservable<T> TriggerSideEffect<T, TSideEffect>(this IObservable<T> source, Func<T, IObservable<TSideEffect>> handlerFactory)
            => source.Publish(bus => bus.SelectMany(item=>handlerFactory(item)
                .TakeUntil(bus.LastOrDefaultAsync().SubscribeOnDefault()
                    .Select(x=>x)).IgnoreElements().To(item).StartWith(item)));
        
        public static IObservable<T> TriggerSideEffect<T, TSide>(this IObservable<T> source, IObservable<TSide> sideEffect) 
            => source.Publish(bus => bus.TakeUntil(bus.LastOrDefaultAsync().Zip(sideEffect.LastOrDefaultAsync())));

    }
}