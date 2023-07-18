using System;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Conditional {
    public static partial class Conditional {
        public static IObservable<TSource> SkipUntil<TSource>(this IObservable<TSource> source, Func<TSource, bool> predicate) 
            => source.Publish(s => s.SkipUntil(s.Where(predicate)));
    }
}