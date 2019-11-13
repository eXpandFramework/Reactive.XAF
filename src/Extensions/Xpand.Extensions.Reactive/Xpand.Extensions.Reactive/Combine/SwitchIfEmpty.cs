using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Combine{
    public static partial class Combine{
        public static IObservable<T> SwitchIfEmpty<T>(this IObservable<T> @this, IObservable<T> switchTo){
            if (@this == null) throw new ArgumentNullException(nameof(@this));
            if (switchTo == null) throw new ArgumentNullException(nameof(switchTo));
            return Observable.Create<T>(obs => {
                var source = @this.Replay(1);
                var switched = source.Any().SelectMany(any => any ? Observable.Empty<T>() : switchTo);
                return new CompositeDisposable(source.Concat(switched).Subscribe((IObserver<T>) obs), source.Connect());
            });
        }
    }
}