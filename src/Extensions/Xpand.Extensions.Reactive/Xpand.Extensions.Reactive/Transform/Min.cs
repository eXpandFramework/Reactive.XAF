using System;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Transform {
    public static partial class Transform {
        public static IObservable<decimal> LiveMin(this IObservable<decimal> source) 
            => source.Scan(decimal.MaxValue, Math.Min);

        public static IObservable<int> LiveMin(this IObservable<int> source)
            => source.Scan(int.MaxValue, Math.Min);

        public static IObservable<double> LiveMin(this IObservable<double> source) 
            => source.Scan(double.MaxValue, Math.Min);
    }
}