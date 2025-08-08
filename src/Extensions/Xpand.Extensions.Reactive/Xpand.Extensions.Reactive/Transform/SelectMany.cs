using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Reactive.ErrorHandling;

namespace Xpand.Extensions.Reactive.Transform {
    public static partial class Transform {
        public static IObservable<TSource> SelectMany<TSource>(this IObservable<IObservable<TSource>> source) 
            => source.SelectMany(source1 => source1);
        
        public static IObservable<TSource> SelectMany<TSource>(this IObservable<IEnumerable<TSource>> source) 
            => source.SelectMany(source1 => source1.ToNowObservable());
        public static IObservable<TSource> SelectMany<TSource>(this IObservable<IEnumerable<TSource>> source,int take) 
            => source.SelectMany(source1 => source1.Take(take).ToNowObservable());
        public static IObservable<TSource> SelectMany<TSource>(this IObservable<IAsyncEnumerable<TSource>> source) 
            => source.SelectMany(source1 => source1.ToObservable());


        public static IObservable<TResult> SelectManyItemResilient<TSource, TResult>(this IObservable<TSource> source, Func<TSource, IObservable<TResult>> resilientSelector,[CallerMemberName]string caller="")
            => source.SelectManyItemResilient(resilientSelector,[],caller);
        public static IObservable<TResult> SelectManyItemResilient<TSource, TResult>(this IObservable<TSource> source, Func<TSource, IEnumerable<TResult>> resilientSelector,[CallerMemberName]string caller="")
            => source.SelectManyItemResilient(arg => resilientSelector(arg).ToNowObservable(),[],caller);
        
        public static IObservable<TResult> SelectManyItemResilient<TSource, TResult>(this IObservable<TSource> source, Func<TSource, IObservable<TResult>> resilientSelector,object[] context,[CallerMemberName]string caller="")
            => source.SelectMany(arg => resilientSelector(arg).ContinueOnError((context ?? Enumerable.Empty<object>()).Concat(((object)arg).YieldItem()).ToArray(), caller));
    }
}