using System.Collections.Generic;
using System.Linq;

namespace Xpand.Extensions.Linq{
    public static partial class LinqExtensions{
        public static IEnumerable<TSource> Add<TSource>(this IEnumerable<TSource> source,TSource item){
            return source.Concat(new[]{item});
        }

    }
}