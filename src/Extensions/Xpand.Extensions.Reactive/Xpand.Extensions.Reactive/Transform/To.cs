using System;
using System.Reactive;
using System.Reactive.Linq;
using Xpand.Extensions.ObjectExtensions;
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
        private static ResilientObservable<TSource> WhenNotDefault<TSource>(this ResilientObservable<TSource> source) 
            => source.Where(s => !s.IsDefaultValue());

        public static ResilientObservable<T1> ToFirst<T1, T2>(this ResilientObservable<(T1, T2)> source)
            => source.Select(tuple => tuple.Item1);

        public static ResilientObservable<T2> ToSecond<T1, T2>(this ResilientObservable<(T1, T2)> source)
            => source.Select(tuple => tuple.Item2);

        public static ResilientObservable<T1> ToFirst<T1, T2, T3>(this ResilientObservable<(T1, T2, T3)> source)
            => source.Select(tuple => tuple.Item1);

        public static ResilientObservable<T2> ToSecond<T1, T2, T3>(this ResilientObservable<(T1, T2, T3)> source)
            => source.Select(tuple => tuple.Item2);

        public static ResilientObservable<T3> ToThird<T1, T2, T3>(this ResilientObservable<(T1, T2, T3)> source)
            => source.Select(tuple => tuple.Item3);

        public static ResilientObservable<T1> ToFirst<T1, T2, T3, T4>(this ResilientObservable<(T1, T2, T3, T4)> source)
            => source.Select(tuple => tuple.Item1);

        public static ResilientObservable<T2> ToSecond<T1, T2, T3, T4>(this ResilientObservable<(T1, T2, T3, T4)> source)
            => source.Select(tuple => tuple.Item2);

        public static ResilientObservable<T3> ToThird<T1, T2, T3, T4>(this ResilientObservable<(T1, T2, T3, T4)> source)
            => source.Select(tuple => tuple.Item3);

        public static ResilientObservable<T4> ToFourth<T1, T2, T3, T4>(this ResilientObservable<(T1, T2, T3, T4)> source)
            => source.Select(tuple => tuple.Item4);

        public static ResilientObservable<T1> ToFirst<T1, T2, T3, T4, T5>(this ResilientObservable<(T1, T2, T3, T4, T5)> source)
            => source.Select(tuple => tuple.Item1);

        public static ResilientObservable<T2> ToSecond<T1, T2, T3, T4, T5>(this ResilientObservable<(T1, T2, T3, T4, T5)> source)
            => source.Select(tuple => tuple.Item2);

        public static ResilientObservable<T3> ToThird<T1, T2, T3, T4, T5>(this ResilientObservable<(T1, T2, T3, T4, T5)> source)
            => source.Select(tuple => tuple.Item3);

        public static ResilientObservable<T4> ToFourth<T1, T2, T3, T4, T5>(this ResilientObservable<(T1, T2, T3, T4, T5)> source)
            => source.Select(tuple => tuple.Item4);

        public static ResilientObservable<T5> ToFifth<T1, T2, T3, T4, T5>(this ResilientObservable<(T1, T2, T3, T4, T5)> source)
            => source.Select(tuple => tuple.Item5);

        public static ResilientObservable<TValue> To<TSource, TValue>(this ResilientObservable<TSource> source, TValue value)
            => source.Select(_ => value);

        public static ResilientObservable<T> To<T>(this ResilientObservable<object> source)
            => source.Select(o => o is T arg ? arg : default);

        public static ResilientObservable<T> To<T>(this ResilientObservable<Unit> source)
            => source.Select(_ => default(T));

        public static ResilientObservable<T> To<T, TResult>(this ResilientObservable<TResult> source)
            => source.Select(_ => default(T)).WhenNotDefault();
        
    }
}