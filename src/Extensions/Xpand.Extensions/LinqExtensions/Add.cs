using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Xpand.Extensions.LinqExtensions{
    public static partial class LinqExtensions{
	    [PublicAPI]
	    public static void AddRange(this  IList list,IEnumerable enumerable){
            foreach (var o in enumerable){
                list.Add(o);
            }
        }

        public static TSource[] Add<TSource>(this IEnumerable<TSource> source,TSource item){
            source ??= Enumerable.Empty<TSource>();
            return source.Concat(new[]{item}).ToArray();
        }

    }
}