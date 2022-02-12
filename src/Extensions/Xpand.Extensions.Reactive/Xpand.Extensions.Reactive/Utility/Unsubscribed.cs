using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Utility {
    public static partial class Utility {
        public static IObservable<T> Unsubscribed<T>(this IObservable<T> source, Action unsubscribed) 
            => Observable.Create<T>(o => new CompositeDisposable(source.Subscribe(o), Disposable.Create(unsubscribed)));
    }
}