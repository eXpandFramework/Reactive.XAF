using System;
using System.Reactive.Linq;
using Xpand.Extensions.Reactive.ErrorHandling;

namespace Xpand.Extensions.Reactive.Utility {
    
    public static partial class Utility {
        public static IObservable<TResult> Use<T, TResult>(this T source, Func<T, IObservable<TResult>> selector) where T : IDisposable
            => source.UsingResilient(() => source, selector);
        
        public static IObservable<T> RunSideEffect<T, TSide>(this IObservable<T> source, IObservable<TSide> sideEffect, bool propagateSideEffectError = false) => source.Publish(bus => {
            var safeSideEffect = propagateSideEffectError ? sideEffect
                : sideEffect.OnErrorResumeNext(Observable.Empty<TSide>());
            return bus.TakeUntil(bus.LastOrDefaultAsync()
                .Zip(safeSideEffect.LastOrDefaultAsync()));
        });

        public static IObservable<TResult> UsingResilient<TResource, TResult>(this object _,
            Func<TResource> resourceFactory, Func<TResource, IObservable<TResult>> observableFactory) where TResource : IDisposable 
            => Observable.Defer(() => {
                TResource resource; try {
                    resource = resourceFactory();
                }
                catch (Exception ex) {
                    return ex.Tag().Publish<TResult>();
                }
                return Observable.Using(() => resource, observableFactory.WithResilience());
            });


    }
}