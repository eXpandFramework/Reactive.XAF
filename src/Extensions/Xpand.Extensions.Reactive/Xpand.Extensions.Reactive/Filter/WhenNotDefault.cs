using System;
using System.Collections;
using System.Reactive.Linq;
using Xpand.Extensions.ObjectExtensions;
using Xpand.Extensions.StringExtensions;

namespace Xpand.Extensions.Reactive.Filter{
    public static partial class Filter {
        public static IObservable<TSource> WhenIncludeNotDefault<TSource, TValue>(this IObservable<TSource> source,
            TSource value, Func<TSource, TValue> valueSelector)
            => source.StartWith(value).WhenNotDefault(valueSelector);
        
	    public static IObservable<TSource> WhenNotDefault<TSource,TValue>(this IObservable<TSource> source,Func<TSource,TValue> valueSelector) 
            =>source.Where(source1 => !valueSelector(source1).IsDefaultValue());

        public static IObservable<TSource> WhenNotDefault<TSource>(this IObservable<TSource> source) => source.Where(s => !s.IsDefaultValue());
        
        public static IObservable<TSource> WhenNotDefaultOrEmpty<TSource>(this IObservable<TSource> source) where TSource:IEnumerable
            => source.WhenNotDefault().WhenNotEmpty();
        
        public static IObservable<string> WhenNotDefaultOrEmpty(this IObservable<string> source) => source.Where(s => !s.IsNullOrEmpty());
        
        public static IObservable<string> WhenDefaultOrEmpty(this IObservable<string> source) => source.Where(s => s.IsNullOrEmpty());
    }
}