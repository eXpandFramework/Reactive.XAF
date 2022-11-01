using System;
using System.Reactive.Linq;
using Xpand.Extensions.ObjectExtensions;

namespace Xpand.Extensions.Reactive.Filter{
    public static partial class Filter{
	    public static IObservable<TSource> WhenDefault<TSource>(this IObservable<TSource> source) 
		    => source.Where(obj => obj.IsDefaultValue());

	    public static IObservable<TSource> WhenDefault<TSource,TValue>(this IObservable<TSource> source,Func<TSource, TValue> valueSelector) 
		    =>source.Where(source1 => valueSelector(source1).IsDefaultValue());
	    
	    public static IObservable<TSource> WhenDefault<TSource>(this IObservable<TSource> source,Func<TSource, object> valueSelector,Func<TSource,Type> valueType) 
		    =>source.Where(source1 => valueSelector(source1).IsDefaultValue());
    }
}