using System;
using System.Reactive.Linq;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.Extensions.Reactive.Conditional {
    public static partial class Conditional {
        public static IObservable<T> TakeUntilCompleted<T,T2>(this IObservable<T> source, IObservable<T2> next)
            => source.TakeUntil(next.WhenCompleted());    }
}