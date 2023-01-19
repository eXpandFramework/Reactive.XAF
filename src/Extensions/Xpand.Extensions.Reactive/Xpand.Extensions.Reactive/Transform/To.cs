using System;
using System.Reactive;
using System.Reactive.Linq;
using Xpand.Extensions.Reactive.Filter;

namespace Xpand.Extensions.Reactive.Transform{
    public static partial class Transform{
        public static IObservable<TValue> To<TSource,TValue>(this IObservable<TSource> source,TValue value) 
            => source.Select(_ => value);

        public static IObservable<T> To<T>(this IObservable<object> source) 
            => source.Select(o =>o is T arg? arg: default);
        public static IObservable<T> To<T>(this IObservable<Unit> source) 
            => source.Select(_ => default(T));
        

        public static IObservable<T> To<T,TResult>(this IObservable<TResult> source) 
            => source.Select(_ => default(T)).WhenNotDefault();
    }
}