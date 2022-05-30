using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Transform {
    public static partial class Transform {
        public static IObservable<double> LiveAverage(this IObservable<double> source)
            => source.Publish(p => p.LiveCount().Zip(p.LiveSum(), (a, b) => b / a));

        public static IObservable<decimal> LiveAverage(this IObservable<decimal> source) 
            => source.Publish(p => p.LiveCount().Zip(p.LiveSum(), (a, b) => b / a));

        public static IObservable<double> LiveAverage<T>(this IObservable<T> source, Func<T, int> func) 
            => source.Publish(p => p.LiveCount().Zip(p.LiveSum(func), (a, b) => (double) b / a));

        public static IObservable<decimal> LiveAverage<T>(this IObservable<T> source, Func<T, decimal> func) 
            => source.Publish(p => p.LiveCount().Zip(p.LiveSum(func), (a, b) => b / a));

        public static IObservable<double> LiveAverage<T>(this IObservable<T> source, Func<T, double> func) 
            => source.Publish(p => p.LiveCount().Zip(p.LiveSum(func), (a, b) => b / a));
        
    }
}