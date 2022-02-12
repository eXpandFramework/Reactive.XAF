using System;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Combine {
    public static partial class Combine {
        public static IObservable<T> DelayIfEmpty<T>(this IObservable<T> source, TimeSpan timeSpan)
            => source.SwitchIfEmpty(Observable.Defer(() => Observable.Timer(timeSpan).SelectMany(_ => Observable.Empty<T>())));
    }
}