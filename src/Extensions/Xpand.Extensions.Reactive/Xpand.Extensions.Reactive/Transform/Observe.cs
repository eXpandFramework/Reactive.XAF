using System;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Transform {
    public static partial class Transform {
        public static IObservable<T> MergeIgnored<T,T2>(this IObservable<T> source,Func<T,IObservable<T2>> secondSelector)
            => source.SelectMany(arg => secondSelector(arg).IgnoreElements().To(arg).StartWith(arg));

        public static IObservable<T> ConcatIgnored<T,T2>(this IObservable<T> source,Func<T,IObservable<T2>> secondSelector)
            => source.SelectMany(arg => secondSelector(arg).IgnoreElements().To(arg).Concat(arg.ReturnObservable()));
        
    }
}