using System;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Utility {
    public static partial class Utility {
        public static IObservable<T> Take<T>(this IObservable<T> source, int count, bool ignoreZeroCount)
            => ignoreZeroCount && count == 0 ? source : source.Take(count);
    }
}