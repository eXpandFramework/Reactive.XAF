using System;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Filter {
    public static partial class Filter {
        public static IObservable<TSource> ExactType<TSource>(this IObservable<object> source)
            => source.WhenNotDefault().OfType<TSource>().Where(source1 => source1.GetType() == typeof(TSource));
    }
}