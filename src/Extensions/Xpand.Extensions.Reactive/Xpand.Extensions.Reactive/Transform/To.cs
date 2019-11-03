using System;
using System.Reactive.Linq;
using Xpand.Extensions.Reactive.Filter;

namespace Xpand.Extensions.Reactive.Transform{
    public static partial class Transform{
        public static IObservable<TValue> To<TSource,TValue>(this IObservable<TSource> source,TValue value){
            return source.Select(o => value);
        }

        public static IObservable<T> To<T>(this IObservable<object> source){
            return source.Select(o => default(T)).WhenNotDefault();
        }
    }
}