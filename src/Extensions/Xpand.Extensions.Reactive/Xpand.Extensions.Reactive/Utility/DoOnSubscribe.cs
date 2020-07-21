using System;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Utility{
    public static partial class Utility{
        public static IObservable<T> DoOnSubscribe<T>(this IObservable<T> source, Action action) 
            => Observable.Defer(() => {
                action();
                return source;
            });
    }
}