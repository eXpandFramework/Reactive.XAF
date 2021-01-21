using System;
using System.Reactive.Linq;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.Extensions.Reactive.Utility {
    public static partial class Utility {
        public static IObservable<T> DelayRandomly<T>(this IObservable<T> source, int maxValue, int minValue = 0)
            => source.SelectMany(arg => {
                var value = Random.Next(minValue, maxValue);
                return value == 0 ? arg.ReturnObservable() : Observable.Timer(TimeSpan.FromSeconds(value)).To(arg);
            });
    }
}