using System.Collections.Generic;

namespace Xpand.Extensions.LinqExtensions{
    public static partial class LinqExtensions{
        public static IEnumerable<T> YieldAll<T>(this IEnumerable<T> source) {
            return source;
        }
        public static IEnumerable<TSource> YieldItem<TSource>(this TSource source){
            yield return source;
        }
    }
}