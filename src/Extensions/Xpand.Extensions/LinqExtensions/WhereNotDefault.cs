using System;
using System.Collections.Generic;
using System.Linq;
using Fasterflect;
using Xpand.Extensions.ObjectExtensions;

namespace Xpand.Extensions.LinqExtensions{
    public static partial class LinqExtensions{
        public static IEnumerable<T> WhereNotDefault<T,T2>(this IEnumerable<T> source, Func<T,T2> predicate) => source.Where(arg => !predicate(arg).IsDefaultValue());
        public static IEnumerable<TSource> WhereNotDefault<TSource>(this IEnumerable<TSource> source) {
            var type = typeof(TSource);
            if (type.IsClass || type.IsInterface){
                return source.Where(source1 => source1!=null);   
            }
            var instance = type.CreateInstance();
            return source.Where(source1 => !source1.Equals(instance));
        }

        
    }
}