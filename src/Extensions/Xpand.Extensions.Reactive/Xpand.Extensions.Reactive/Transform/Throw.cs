using System;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Transform {
    public static partial class Transform {
        public static IObservable<T> Throw<T>(this Exception exception)
            => Observable.Throw<T>(exception);
    }
}