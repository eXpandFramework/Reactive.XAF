using System;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Filter{
    public static partial class Filter{

        public static IObservable<TSource> WhenDefault<TSource>(this IObservable<TSource> source){
            return source.Where(_ => {
                var def = default(TSource);
                return def != null && def.Equals(_);
            });
        }
    }
}