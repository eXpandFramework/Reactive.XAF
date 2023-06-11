using System;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Utility {
    public static partial class Utility {
        
        public static IObservable<TSource> Timeout<TSource>(
            this IObservable<TSource> source, TimeSpan dueTime, string timeoutMessage) 
            => source.Timeout(dueTime, Observable.Throw<TSource>(new TimeoutException(timeoutMessage)));
    }
}