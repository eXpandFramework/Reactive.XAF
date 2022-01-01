using System;
using System.Linq;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Combine {
    public static partial class Combine {
        public static IObservable<TResult> CombineVeryLatest<TLeft, TRight, TResult>(this IObservable<TLeft> leftSource,
            IObservable<TRight> rightSource, Func<TLeft, TRight, TResult> selector) 
            => Observable.Defer(() => {
                int l = -1, r = -1;
                return leftSource.Select(Tuple.Create<TLeft, int>).CombineLatest(
                        rightSource.Select(Tuple.Create<TRight, int>),
                        (x, y) => new { x, y })
                    .Where(t => t.x.Item2 != l && t.y.Item2 != r)
                    .Do(t => {
                        l = t.x.Item2;
                        r = t.y.Item2;
                    })
                    .Select(t => selector(t.x.Item1, t.y.Item1));
            });
    }
}