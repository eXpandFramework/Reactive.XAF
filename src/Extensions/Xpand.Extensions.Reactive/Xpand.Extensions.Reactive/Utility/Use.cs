using System;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using Xpand.Extensions.Reactive.ErrorHandling;

namespace Xpand.Extensions.Reactive.Utility {
    
    public static partial class Utility {
        public static IObservable<TResult> Use<T, TResult>(this T source, Func<T, IObservable<TResult>> selector,Func<IObservable<TResult>, IObservable<TResult>> retrySelector = null) where T : IDisposable {
            var baseSequence = selector(source);
            var resilientSequence = retrySelector != null ? retrySelector(baseSequence) : baseSequence;
            return resilientSequence.Finally(source.Dispose);
        }

        public static IObservable<T> RunSideEffect<T, TSide>(this IObservable<T> source, IObservable<TSide> sideEffect, bool propagateSideEffectError = false) => source.Publish(bus => {
            var safeSideEffect = propagateSideEffectError ? sideEffect
                : sideEffect.OnErrorResumeNext(Observable.Empty<TSide>());
            return bus.TakeUntil(bus.LastOrDefaultAsync()
                .Zip(safeSideEffect.LastOrDefaultAsync()));
        });

        public static IObservable<TResult> Using<TResource, TResult>(this object _,
            Func<TResource> resourceFactory, Func<TResource, IObservable<TResult>> busFactory,[CallerMemberName]string caller="") where TResource : IDisposable 
            => Observable.Using(resourceFactory, arg => busFactory(arg).ChainFaultContext([caller,typeof(TResult)]));
    }
}