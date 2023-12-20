using System;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Filter {
    public static partial class Filter {
        public static IObservable<T> Take<T>(this IObservable<T> source, int count, bool ignoreZeroCount)
            => ignoreZeroCount && count == 0 ? source : source.Take(count);
        public static IObservable<T> TakeWhen<T>(this IObservable<T> source, int count, bool when)
            => when?source.Take(count):source;
        public static IObservable<T> SkipWhen<T>(this IObservable<T> source, int count, bool when)
            => when?source.Skip(count):source;

        public static IObservable<T> Skip<T>(this IObservable<T> source, int count, bool ignoreZeroCount) 
            => ignoreZeroCount && count == 0 ? source : source.Skip(count);
    }
}