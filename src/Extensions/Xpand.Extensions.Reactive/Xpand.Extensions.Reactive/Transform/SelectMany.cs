using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Transform {
    public static partial class Transform {
        public static IObservable<TSource> SelectMany<TSource>(this IObservable<IObservable<TSource>> source) 
            => source.SelectMany(source1 => source1);
        
        public static IObservable<TSource> SelectMany<TSource>(this IObservable<IEnumerable<TSource>> source) 
            => source.SelectMany(source1 => source1.ToNowObservable());
        public static IObservable<TSource> SelectMany<TSource>(this IObservable<IEnumerable<TSource>> source,int take) 
            => source.SelectMany(source1 => source1.Take(take).ToNowObservable());
        
        // public static IObservable<object> SelectMany(this IObservable<IEnumerable> source) 
        //     => source.SelectMany(source1 => source1.Cast<object>());
        //
        // public static IEnumerable<object> SelectMany(this IEnumerable<IEnumerable> source) 
        //     => source.SelectMany(source1 => source1.Cast<object>());
    }
}