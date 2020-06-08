using System;
using System.Reactive.Linq;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.Extensions.Reactive.Filter{
    public static partial class Filter{
	    public static IObservable<TSource> WhenDefault<TSource>(this IObservable<TSource> source,Func<TSource,object> predicate) =>source
		    .SelectMany(source1 => predicate(source1).ReturnObservable().WhenDefault().To(source1));

        public static IObservable<TSource> WhenDefault<TSource>(this IObservable<TSource> source) =>
            source.Where(_ => {
                var def = default(TSource);
                return def != null && def.Equals(_);
            });
    }
}