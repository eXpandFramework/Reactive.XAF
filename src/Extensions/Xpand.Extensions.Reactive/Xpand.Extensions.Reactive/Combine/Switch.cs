using System;
using System.Collections.Generic;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Combine{
    public static partial class Combine{
        public static IObservable<T> SwitchIfEmpty<T>(this IObservable<T> source, IObservable<T> switchTo) 
            => source.Publish(published =>
                published.Merge(published.IsEmpty().Where(isEmpty => isEmpty).SelectMany(_ => switchTo))
            );

        public static IObservable<T> SwitchIfDefault<T>(this IObservable<T> source, IObservable<T> switchTo)  
            => source.Select(entry => !EqualityComparer<T>.Default.Equals(entry, default) ? Observable.Return(entry) : switchTo)
                .TakeUntil(stream => stream == switchTo)
                .Concat();
        
        
    }
}