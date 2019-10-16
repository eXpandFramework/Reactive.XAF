using System.Collections.Generic;
using System.Linq;

namespace Xpand.Extensions.Linq{
    public static partial class LinqExtensions{
        public static void AddRange<TSource>(this IList<TSource> source, IEnumerable<TSource> items){
            foreach (var item in items){
                source.Add(item);
            }
        }

        public static IEnumerable<TSource> Add<TSource>(this IEnumerable<TSource> source,TSource item){
            return source.Concat(new[]{item});
        }

    }
}