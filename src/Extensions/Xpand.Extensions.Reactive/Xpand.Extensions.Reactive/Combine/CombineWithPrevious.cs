using System;
using System.Reactive.Linq;
using Xpand.Extensions.ObjectExtensions;

namespace Xpand.Extensions.Reactive.Combine{
    public static partial class Combine{
        public static IObservable<(TSource previous, TSource current)> CombineWithPrevious<TSource>(this IObservable<TSource> source,bool ensurePrevious=false) 
            => source.Scan((previous: default(TSource), current: default(TSource)), (_, current) => (_.current, current))
                .Select(t => (t.previous, t.current)).Where(t => !ensurePrevious||!t.previous.IsDefaultValue());
        public static IObservable<TSource> CombineWithPrevious<TSource>(this IObservable<TSource> source,Func<(TSource previous,TSource current),TSource> selector,bool emitPrevious=false) 
            => source.CombineWithPrevious(!emitPrevious).Select(selector);
        
        public static IObservable<(TSource previous, TSource current)> CombineWithPrevious<TSource>(this IObservable<TSource> source,Func<(TSource previous,TSource current),bool> selector,bool emitPrevious=false) 
            => source.CombineWithPrevious(!emitPrevious).Where(selector);
        
        public static IObservable<TSource> ToCurrent<TSource>(this IObservable<(TSource previous, TSource current)> source) 
            => source.Select(t => t.current);
    }
}