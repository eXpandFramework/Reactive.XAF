using System;
using System.Linq;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Filter {
    public static partial class Filter {
        public static IObservable<TSource> Except<TSource>(this IObservable<TSource> source, params Type[] types)
            => source.Where(o => types.All(type => (o?.GetType() ?? typeof(TSource)) != type));
    }
}