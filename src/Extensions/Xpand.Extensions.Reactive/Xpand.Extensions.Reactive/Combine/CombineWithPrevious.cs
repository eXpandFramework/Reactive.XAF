using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Combine{
    public static partial class Combine{
        public static IObservable<(TSource previous, TSource current)> CombineWithPrevious<TSource>(
            this IObservable<TSource> source){
            return source
                .Scan((previous: default(TSource), current: default(TSource)), (_, current) => (_.current, current))
                .Select(t => (t.previous, t.current));
        }
    }
}