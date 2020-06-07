using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Xpand.Extensions.LinqExtensions{
    public static partial class LinqExtensions{
	    // public static int GetValidIndex<T>(this IList<T> array, int index) {
		   //  return Math.Max(0, Math.Min(index, array.Count - 1));
	    // }
	    // public static bool TryGetValue<T>(this IList<T> array, int index, out T value) {
		   //  if(IsValidIndex(array, index)) {
			  //   value = array[index];
			  //   return true;
		   //  }
		   //  value = default(T);
		   //  return false;
	    // }
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