using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Transform{
    public static partial class Transform{
        public static IObservable<Unit> ToUnit<T>(this IObservable<T> source) 
            => source.Select(_ => Unit.Default);
        public static IEnumerable<Unit> ToUnit<T>(this IEnumerable<T> source) 
            => source.Select(_ => Unit.Default);
        
        public static IObservable<Unit> ToUnit<T1, T2>(this IObservable<(T1, T2)> source) 
            => source.Select(_ => Unit.Default);

        public static IObservable<Unit> ToUnit<T1, T2, T3>(this IObservable<(T1, T2, T3)> source) 
            => source.Select(_ => Unit.Default);

        public static IObservable<Unit> ToUnit<T1, T2, T3, T4>(this IObservable<(T1, T2, T3, T4)> source) 
            => source.Select(_ => Unit.Default);

        public static IObservable<Unit> ToUnit<T1, T2, T3, T4, T5>(this IObservable<(T1, T2, T3, T4, T5)> source) 
            => source.Select(_ => Unit.Default);

    }
}