using System;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Conditional {
    public static partial class Conditional {
        public static IObservable<T> TakeUntilInclusive<T>(this IObservable<T> source, Func<T, bool> predicate) 
            => source.Publish(co => co.TakeUntil(predicate).Merge(co.SkipUntil(predicate).Take(1)));
    }
    
    
}