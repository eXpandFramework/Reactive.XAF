using System;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Utility {
    public static partial class Utility {
        public static IObservable<T> Start<T>(this IObservable<T> source)
            => source.SelectMany(arg => Observable.Start(() => arg));
    }
}