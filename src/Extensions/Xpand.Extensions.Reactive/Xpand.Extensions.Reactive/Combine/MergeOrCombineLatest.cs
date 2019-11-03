using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Combine{
    public static partial class Combine{
        public static IObservable<TC> MergeOrCombineLatest<TA, TB, TC>(
            this IObservable<TA> a,
            IObservable<TB> b,
            Func<TA, TC> aResultSelector, // When A starts before B
            Func<TB, TC> bResultSelector, // When B starts before A
            Func<TA, TB, TC> bothResultSelector) // When both A and B have started
        {
            return
                a.Publish(aa =>
                    b.Publish(bb =>
                        aa.CombineLatest(bb, bothResultSelector).Publish(xs =>
                            aa
                                .Select(aResultSelector)
                                .Merge(bb.Select(bResultSelector))
                                .TakeUntil(xs)
                                .SkipLast(1)
                                .Merge(xs))));
        }

    }
}