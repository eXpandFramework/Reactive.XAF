using System;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Utility {
    public static partial class Utility {
        public static IObservable<T2> Switch<T,T2>(this IObservable<T> source, Func<T, IObservable<T2>> selector)
            => source.Select(selector).Switch();
    }
}