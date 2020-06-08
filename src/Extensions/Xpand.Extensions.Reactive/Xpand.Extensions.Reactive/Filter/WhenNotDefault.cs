using System;
using System.Reactive.Linq;
using Xpand.Extensions.ObjectExtensions;

namespace Xpand.Extensions.Reactive.Filter{
    public static partial class Filter{
	    public static IObservable<TSource> WhenNotDefault<TSource,TValue>(this IObservable<TSource> source,Func<TSource,TValue> valueSelector) =>source
	        .Where(source1 => !valueSelector(source1).IsDefaultValue());

        public static IObservable<TSource> WhenNotDefault<TSource>(this IObservable<TSource> source) => source.Where(_ => !_.IsDefaultValue());
    }
}