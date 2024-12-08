using System;
using System.Collections.Generic;
using System.Linq;

namespace Xpand.Extensions.LinqExtensions {
    public static partial class LinqExtensions {
        public static IEnumerable<object> ExactType(this IEnumerable<object> source,Type objectType) 
            => source.Where(arg => arg.GetType()==objectType);
        
        public static IEnumerable<T> ExactType<T>(this IEnumerable<T> source,Type objectType,Func<T,Type> typeSelector) 
            => source.Where(arg => (typeSelector?.Invoke(arg) ?? arg.GetType())==objectType);

        public static IEnumerable<T> ExactType<T>(this IEnumerable<object> source)
            => source.OfType<T>().Where(arg => arg.GetType() == typeof(T));
    }
}