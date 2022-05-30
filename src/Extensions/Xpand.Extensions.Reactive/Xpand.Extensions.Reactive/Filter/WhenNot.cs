using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Filter {
    public static partial class Filter {
        public static IObservable<TSource> WhenNot<TSource>(this IObservable<TSource> source, Func<TSource, bool> filter) 
            => source.Where(source1 => !filter(source1));
    }
}