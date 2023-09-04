using System;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Conditional {
    public static partial class Conditional {
        public static IObservable<TResult> If<TSource, TResult>(this IObservable<TSource> source,
            Func<int,TSource, bool> predicate, Func<TSource, IObservable<TResult>> thenSource, Func<TSource, IObservable<TResult>> elseSource) 
            => source.SelectMany((value, i) => predicate(i,value) ? thenSource(value) : elseSource(value));

        public static IObservable<TResult> If<TSource, TResult>(this IObservable<TSource> source,
            Func<TSource, bool> predicate, Func<TSource, IObservable<TResult>> thenSource, Func<TSource, IObservable<TResult>> elseSource) 
            => source.SelectMany(value => predicate(value) ? thenSource(value) : elseSource(value));

        public static IObservable<TResult> If<TSource, TResult>(this IObservable<TSource> source,
            Func<TSource, bool> predicate, Func<TSource, IObservable<TResult>> thenSource) 
            => source.SelectMany(value => predicate(value) ? thenSource(value) :Observable.Empty<TResult>());
        
        public static IObservable<TResult> If<TSource, TResult>(this IObservable<TSource> source,
            Func<TSource, bool> predicate, Func<IObservable<TResult>> thenSource, Func< IObservable<TResult>> elseSource) 
            => source.If(predicate, _ => thenSource(),_ => elseSource());
        
        
    }
}