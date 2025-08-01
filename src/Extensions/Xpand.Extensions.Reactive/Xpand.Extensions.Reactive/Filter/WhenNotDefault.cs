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

        public static IObservable<TSource> WhenNotDefault<TSource, TValue>(this IObservable<TSource> source, Func<TSource, TValue> valueSelector)
            => source.Where(source1 => !valueSelector(source1).IsDefaultValue());
        

        public static IObservable<TSource> WhenNotDefault<TSource>(this IObservable<TSource> source) 
            => source.Where(s => !s.IsDefaultValue());
        

        public static IObservable<bool> WhenTrue(this IObservable<bool> source) => source.WhenNotDefault();
        public static IObservable<bool> WhenFalse(this IObservable<bool> source) => source.WhenDefault();
        public static IObservable<int> WhenZero(this IObservable<int> source) => source.WhenDefault();
        public static IObservable<int> WhenNonZero(this IObservable<int> source) => source.WhenNotDefault();
        public static IObservable<long> WhenZero(this IObservable<long> source) => source.WhenDefault();
        public static IObservable<long> WhenNonZero(this IObservable<long> source) => source.WhenNotDefault();
        public static IObservable<decimal> WhenZero(this IObservable<decimal> source) => source.WhenDefault();
        public static IObservable<decimal> WhenNonZero(this IObservable<decimal> source) => source.WhenNotDefault();

        public static IObservable<TSource> WhenNotDefaultOrEmpty<TSource>(this IObservable<TSource> source) where TSource : IEnumerable
            => source.WhenNotDefault().WhenNotEmpty();

        public static IObservable<string> WhenNotDefaultOrEmpty(this IObservable<string> source) 
            => source.Where(s => !s.IsNullOrEmpty());

        public static IObservable<string> WhenDefaultOrEmpty(this IObservable<string> source) 
            => source.Where(s => s.IsNullOrEmpty());
        

        
    }
}