using System;
using System.Reactive.Linq;
using Xpand.Extensions.ObjectExtensions;
using Xpand.Extensions.Reactive.ErrorHandling;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.Extensions.Reactive.Filter{
    public static partial class Filter{
	    public static IObservable<TSource> WhenDefault<TSource>(this IObservable<TSource> source) 
		    => source.Where(obj => obj.IsDefaultValue());

	    public static IObservable<TSource> WhenDefault<TSource,TValue>(this IObservable<TSource> source,Func<TSource, TValue> valueSelector) 
		    =>source.Where(source1 => valueSelector(source1).IsDefaultValue());
	    
	    public static IObservable<TSource> WhenDefault<TSource>(this IObservable<TSource> source,Func<TSource, object> valueSelector,Func<TSource,Type> valueType) 
		    =>source.Where(source1 => valueSelector(source1).IsDefaultValue());
	    
	    public static ResilientObservable<TSource> WhenDefault<TSource>(this ResilientObservable<TSource> source)
		    => source.Where(obj => obj.IsDefaultValue());

	    public static ResilientObservable<TSource> WhenDefault<TSource, TValue>(this ResilientObservable<TSource> source, Func<TSource, TValue> valueSelector)
		    => source.Where(source1 => valueSelector(source1).IsDefaultValue());

	    public static ResilientObservable<TSource> WhenDefault<TSource>(this ResilientObservable<TSource> source, Func<TSource, object> valueSelector, Func<TSource, Type> valueType)
		    => source.Where(source1 => valueSelector(source1).IsDefaultValue());
    }
}