using System;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Transform {
    public static partial class Transform {
        public static IObservable<double> LiveMedian(this IObservable<int> source)
            => source.AccumulateAllOrdered().Select(a => a.Count % 2 == 0 ? (double)(a[a.Count / 2 - 1] + a[a.Count / 2]) / 2 : a[a.Count / 2]);

        public static IObservable<decimal> LiveMedian(this IObservable<decimal> source) 
            => source.AccumulateAllOrdered().Select(a => a.Count % 2 == 0 ? (a[a.Count / 2 - 1] + a[a.Count / 2]) / 2 : a[a.Count / 2]);


        public static IObservable<double> LiveMedian(this IObservable<double> source) 
            => source.AccumulateAllOrdered().Select(a => a.Count % 2 == 0 ? (a[a.Count / 2 - 1] + a[a.Count / 2]) / 2 : a[a.Count / 2]);
        
    }
}