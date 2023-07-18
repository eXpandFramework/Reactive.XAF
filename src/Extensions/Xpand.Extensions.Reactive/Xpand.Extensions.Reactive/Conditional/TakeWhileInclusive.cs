using System;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Conditional {
    public static partial class Conditional {
        public static IObservable<TSource> TakeWhileInclusive<TSource>(this IObservable<TSource> source, Func<TSource, bool> predicate) 
            => source.TakeUntil(source.SkipWhile(predicate).Skip(1));
    }
}