using System;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Transform {
    public static partial class Transform {
        public static IObservable<decimal> LiveSum<T>(this IObservable<T> source, Func<T, decimal> func)
            => source.Scan(0m, (a, b) => a + func(b));

        public static IObservable<decimal> LiveSum(this IObservable<decimal> source) 
            => source.Scan(0m, (a, b) => a + b);

        public static IObservable<double> LiveSum(this IObservable<double> source) 
            => source.Scan(0d, (a, b) => a + b);

        public static IObservable<int> LiveSum<T>(this IObservable<T> source, Func<T, int> func) 
            => source.Scan(0, (a, b) => a + func(b));

        public static IObservable<double> LiveSum<T>(this IObservable<T> source, Func<T, double> func) 
            => source.Scan(0D, (a, b) => a + func(b));
        
        
    }
}