using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Xpand.Extensions.Linq{
    public static partial class LinqExtensions{
        [PublicAPI]
        public static void AddRange(this  IList list,IEnumerable enumerable){
            foreach (var o in enumerable){
                list.Add(o);
            }
        }

        [PublicAPI]
        public static TSource[] AddRange<TSource>(this IEnumerable<TSource> source, IEnumerable<TSource> items){
            return source.Concat(items).ToArray();
        }

        public static TSource[] Add<TSource>(this IEnumerable<TSource> source,TSource item){
            source ??= Enumerable.Empty<TSource>();
            return source.Concat(new[]{item}).ToArray();
        }

    }
}