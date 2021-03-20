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
	    public static void AddRange<T>(this  IList<T> list,IEnumerable<T> enumerable,bool ignoreDuplicates){
            foreach (var o in enumerable.Where(arg =>!ignoreDuplicates|| !list.Contains(arg))){
                list.Add(o);
            }
        }

        public static TSource[] Add<TSource>(this IEnumerable<TSource> source,TSource item){
            source ??= Enumerable.Empty<TSource>();
            return source.Concat(new[]{item}).ToArray();
        }
        
        public static void Add<TSource>(this IList<TSource> source,TSource item,bool ignoreDuplicates){
            if (ignoreDuplicates&&source.Contains(item)) {
                return;
            }

            source.Add(item);
        }

    }
}