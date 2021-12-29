using System;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Utility {
    public static partial class Utility {
        public static IObservable<T> DoOnError<T>(this IObservable<T> source, Action<Exception> onError) 
            => source.Do(_ => { }, onError);
    }
}