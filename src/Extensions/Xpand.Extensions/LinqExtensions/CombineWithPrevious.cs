using System.Collections.Generic;
using System.Linq;

namespace Xpand.Extensions.LinqExtensions {
    public static partial class LinqExtensions {
        public static IEnumerable<(T current, T previous)> CombineWithPrevious<T>(this IEnumerable<T> source) {
            var enumerable = source as T[] ?? source.ToArray();
            return (enumerable.FirstOrDefault(), default(T)).YieldItem()
                .Concat(enumerable.Skip(1).Select((current, i) => (current, previous: enumerable[i])));
        }
    }
}