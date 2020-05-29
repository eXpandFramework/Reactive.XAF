using System;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Combine{
    public static partial class Combine{
        public static IObservable<TC> MergeOrCombineLatest<TA, TB, TC>(this IObservable<TA> a, IObservable<TB> b, Func<TA, TC> aStartsFirst, Func<TB, TC> bStartFirst, Func<TA, TB, TC> bothStart)
            => a.Publish(aa => b
                .Publish(bb => aa.CombineLatest(bb, bothStart)
                    .Publish(xs => aa.Select(aStartsFirst)
                        .Merge(bb.Select(bStartFirst))
                        .TakeUntil(xs)
                        .SkipLast(1)
                        .Merge(xs))));
    }
}