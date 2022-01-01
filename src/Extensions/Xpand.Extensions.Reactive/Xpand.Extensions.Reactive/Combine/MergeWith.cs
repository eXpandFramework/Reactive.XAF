using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Combine{
    public static partial class Combine{
        
        public static IObservable<TValue> MergeWith<TSource, TValue>(this IObservable<TSource> source, TValue value, IScheduler scheduler = null){
            scheduler ??= CurrentThreadScheduler.Instance;
            return source.Merge(Observable.Return(default(TSource), scheduler)).Select(_ => value);
        }
    }
}