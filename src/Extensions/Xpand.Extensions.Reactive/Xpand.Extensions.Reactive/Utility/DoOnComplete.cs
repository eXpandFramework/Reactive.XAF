using System;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Utility {
    public static partial class Utility {
        public static IObservable<T> DoOnComplete<T>(this IObservable<T> source, Action onComplete)
            => source.Do(_ => { }, onComplete);
    }
}