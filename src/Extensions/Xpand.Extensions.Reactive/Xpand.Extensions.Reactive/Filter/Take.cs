using System;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Filter {
    public static partial class Filter {
        public static IObservable<T> Take<T>(this IObservable<T> source, int count, bool ignoreZeroCount)
            => ignoreZeroCount && count == 0 ? source : source.Take(count);
        
        public static IObservable<T> TakeOrOriginal<T>(this IObservable<T> source, int count) 
            => count > 0 ? source.Take(count) : source;
        
        public static IObservable<T> SkipOrOriginal<T>(this IObservable<T> source, int count) 
            => count > 0 ? source.Skip(count) : source;
    }
}