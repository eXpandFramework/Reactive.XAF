using System;
using System.Reactive.Linq;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.Extensions.Reactive.Filter {
    public static partial class Filter {
        public static IObservable<T> TakeOrOriginal<T>(this IObservable<T> source, int count) => count == 0 ? source : source.Take(count);
        public static IObservable<T> SkipOrOriginal<T>(this IObservable<T> source, int count) => count == 0 ? source : source.Skip(count);
        public static IObservable<T> Take<T>(this IObservable<T> source, int count, bool ignoreZeroCount)
            => ignoreZeroCount && count == 0 ? source : source.Take(count);
        public static IObservable<T> TakeWhen<T>(this IObservable<T> source, int count, bool when)
            => when?source.Take(count):source;
        public static IObservable<T> SkipWhen<T>(this IObservable<T> source, int count, bool when)
            => when?source.Skip(count):source;

        public static IObservable<T> Skip<T>(this IObservable<T> source, int count, bool ignoreZeroCount) 
            => ignoreZeroCount && count == 0 ? source : source.Skip(count);
        
        public static ResilientObservable<T> TakeOrOriginal<T>(this ResilientObservable<T> source, int count) 
            => count == 0 ? source : source.Take(count);

        public static ResilientObservable<T> SkipOrOriginal<T>(this ResilientObservable<T> source, int count) 
            => count == 0 ? source : source.Skip(count);

        public static ResilientObservable<T> Take<T>(this ResilientObservable<T> source, int count, bool ignoreZeroCount)
            => ignoreZeroCount && count == 0 ? source : source.Take(count);

        public static ResilientObservable<T> TakeWhen<T>(this ResilientObservable<T> source, int count, bool when)
            => when ? source.Take(count) : source;

        public static ResilientObservable<T> SkipWhen<T>(this ResilientObservable<T> source, int count, bool when)
            => when ? source.Skip(count) : source;

        public static ResilientObservable<T> Skip<T>(this ResilientObservable<T> source, int count, bool ignoreZeroCount) 
            => ignoreZeroCount && count == 0 ? source : source.Skip(count);
    }
}