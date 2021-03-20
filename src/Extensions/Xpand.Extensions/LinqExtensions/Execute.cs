using System;
using System.Collections.Generic;
using System.Linq;

namespace Xpand.Extensions.LinqExtensions {
    public static partial class LinqExtensions {
        public static IEnumerable<TSource> Execute<TSource>(this IEnumerable<TSource> source, Action<TSource> action)
            => source.Select(source1 => {
                action(source1);
                return source1;
            });
    }
}