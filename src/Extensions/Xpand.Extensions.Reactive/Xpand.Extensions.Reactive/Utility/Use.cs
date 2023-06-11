using System;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Utility {
    public static partial class Utility {
        public static IObservable<TResult> Use<T, TResult>(this T source, Func<T, IObservable<TResult>> selector)
            where T : IDisposable
            => Observable.Using(() => source, selector);
    }
}