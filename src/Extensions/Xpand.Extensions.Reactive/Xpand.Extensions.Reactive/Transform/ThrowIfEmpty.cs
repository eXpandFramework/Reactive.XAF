using System;
using System.Reactive.Linq;
using Xpand.Extensions.Reactive.Combine;

namespace Xpand.Extensions.Reactive.Transform {
    public static partial class Transform {
        public static IObservable<T> ThrowIfEmpty<T>(this IObservable<T> source)
            => source.SwitchIfEmpty(Observable.Defer(() => Observable.Throw<T>(new InvalidOperationException("source is empty"))));
    }
}