using System;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Transform{
    public static partial class Transform{
        public static IObservable<T> DoNotComplete<T>(this IObservable<T> source){
            return source.Concat(Observable.Never<T>());
        }
    }
}