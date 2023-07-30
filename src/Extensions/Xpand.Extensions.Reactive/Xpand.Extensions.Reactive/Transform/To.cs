using System;
using System.Reactive;
using System.Reactive.Linq;
using Xpand.Extensions.Reactive.Filter;

namespace Xpand.Extensions.Reactive.Transform{
    public static partial class Transform{
        public static IObservable<T1> ToFirst<T1, T2>(this IObservable<(T1, T2)> source) 
            => source.Select(tuple => tuple.Item1);

        public static IObservable<T2> ToSecond<T1, T2>(this IObservable<(T1, T2)> source) 
            => source.Select(tuple => tuple.Item2);

        public static IObservable<T1> ToFirst<T1, T2, T3>(this IObservable<(T1, T2, T3)> source) 
            => source.Select(tuple => tuple.Item1);

        public static IObservable<T2> ToSecond<T1, T2, T3>(this IObservable<(T1, T2, T3)> source) 
            => source.Select(tuple => tuple.Item2);

        public static IObservable<T3> ToThird<T1, T2, T3>(this IObservable<(T1, T2, T3)> source) 
            => source.Select(tuple => tuple.Item3);

        public static IObservable<T1> ToFirst<T1, T2, T3, T4>(this IObservable<(T1, T2, T3, T4)> source) 
            => source.Select(tuple => tuple.Item1);

        public static IObservable<T2> ToSecond<T1, T2, T3, T4>(this IObservable<(T1, T2, T3, T4)> source) 
            => source.Select(tuple => tuple.Item2);

        public static IObservable<T3> ToThird<T1, T2, T3, T4>(this IObservable<(T1, T2, T3, T4)> source) 
            => source.Select(tuple => tuple.Item3);

        public static IObservable<T4> ToFourth<T1, T2, T3, T4>(this IObservable<(T1, T2, T3, T4)> source) 
            => source.Select(tuple => tuple.Item4);

        public static IObservable<T1> ToFirst<T1, T2, T3, T4, T5>(this IObservable<(T1, T2, T3, T4, T5)> source) 
            => source.Select(tuple => tuple.Item1);

        public static IObservable<T2> ToSecond<T1, T2, T3, T4, T5>(this IObservable<(T1, T2, T3, T4, T5)> source) 
            => source.Select(tuple => tuple.Item2);

        public static IObservable<T3> ToThird<T1, T2, T3, T4, T5>(this IObservable<(T1, T2, T3, T4, T5)> source) 
            => source.Select(tuple => tuple.Item3);

        public static IObservable<T4> ToFourth<T1, T2, T3, T4, T5>(this IObservable<(T1, T2, T3, T4, T5)> source) 
            => source.Select(tuple => tuple.Item4);

        public static IObservable<T5> ToFifth<T1, T2, T3, T4, T5>(this IObservable<(T1, T2, T3, T4, T5)> source) 
            => source.Select(tuple => tuple.Item5);
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