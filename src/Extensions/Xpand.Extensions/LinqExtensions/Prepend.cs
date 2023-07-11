using System.Collections.Generic;
using System.Linq;

namespace Xpand.Extensions.LinqExtensions {
    public static partial class LinqExtensions {
        public static IEnumerable<T> PrependWhenNotEmpty<T>(this IEnumerable<T> source, params T[] values) {
            var array = source as T[] ?? source.ToArray();
            return array.Length > 0 ? values.Concat(array) : array;
        }
    }
}