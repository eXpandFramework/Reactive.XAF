using System.Collections.Generic;
using System.Linq;

namespace Xpand.Extensions.Linq{
    public static partial class LinqExtensions{
        public static TSource[] AddRange<TSource>(this IEnumerable<TSource> source, IEnumerable<TSource> items){
            return source.Concat(items).ToArray();
        }

        public static IEnumerable<TSource> Add<TSource>(this IEnumerable<TSource> source,TSource item){
            return source.Concat(new[]{item});
        }

    }
}