using System;
using System.Reactive.Linq;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.Extensions.Reactive.Utility {
    public static partial class Utility {
        public static IObservable<T> RunSideEffects<T, T2>(this IObservable<T> source, Func<T, IObservable<T2>> handlerFactory)
            => source.SelectMany(item => handlerFactory(item).IgnoreElements().To(item).StartWith(item));
        
        public static IObservable<T> RunSideEffect<T, TSide>(this IObservable<T> source, IObservable<TSide> sideEffect, bool propagateSideEffectError = false) 
            => source.Publish(bus => {
                var safeSideEffect = propagateSideEffectError ? sideEffect : sideEffect.OnErrorResumeNext(Observable.Empty<TSide>());
                return bus.TakeUntil(bus.LastOrDefaultAsync().Zip(safeSideEffect.LastOrDefaultAsync()));
            });

    }
}