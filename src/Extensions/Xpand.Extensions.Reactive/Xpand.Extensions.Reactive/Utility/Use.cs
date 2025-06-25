using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Utility {
    public static partial class Utility {
        public static IObservable<TResult> Use<T, TResult>(this T source, Func<T, IObservable<TResult>> selector)
            where T : IDisposable
            => Observable.Using(() => source, selector);
        
        public static IObservable<T> UseSubscription<T, TSide>(this IObservable<T> source, IObservable<TSide> sideEffect, bool propagateSideEffectError = false)
            =>  Observable.Create<T>(observer => new CompositeDisposable(source.Subscribe(observer), propagateSideEffectError
                ? sideEffect.Subscribe(_ => { }, observer.OnError)
                : sideEffect.Subscribe()));
    }
}