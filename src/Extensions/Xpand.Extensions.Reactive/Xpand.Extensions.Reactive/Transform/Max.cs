
using System;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Transform {
    public static partial class Transform {
        public static IObservable<decimal> LiveMax(this IObservable<decimal> source) 
            => source.Scan(decimal.MaxValue, Math.Max);

        public static IObservable<int> LiveMax(this IObservable<int> source)
            => source.Scan(int.MaxValue, Math.Max);

        public static IObservable<double> LiveMax(this IObservable<double> source) 
            => source.Scan(double.MaxValue, Math.Max);
    }
}