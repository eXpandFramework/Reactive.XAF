using System;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Transform {
    public static partial class Transform {
        public static IObservable<T2> SelectManyConcurrent<T1, T2>(this IObservable<T1> source,
            Func<T1, IObservable<T2>> selector, int concurrency)
            => source.Select(x => Observable.Defer(() => selector(x))).Merge(concurrency);
        
        public static IObservable<T2> SelectManyCPU<T1, T2>(this IObservable<T1> source, Func<T1, IObservable<T2>> selector)
            => source.SelectManyConcurrent(selector, Environment.ProcessorCount);
    }
}