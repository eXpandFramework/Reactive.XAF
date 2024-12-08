using System;
using System.Collections.Generic;
using System.Linq;

namespace Xpand.Extensions.LinqExtensions {
    public static partial class LinqExtensions {
        public static IEnumerable<T> ExceptType<T>(this IEnumerable<T> source,Type objectType) 
            => source.Where(arg => !objectType.IsAssignableFrom(arg.GetType()));
        
        public static IEnumerable<T> ExceptType<T>(this IEnumerable<T> source,Type objectType,Func<T,Type> typeSelector) 
            => source.Where(arg => (typeSelector?.Invoke(arg) ?? arg.GetType())==objectType);

        
    }
}