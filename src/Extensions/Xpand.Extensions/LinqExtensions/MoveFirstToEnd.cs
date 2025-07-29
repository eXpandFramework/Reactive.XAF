using System.Collections.Generic;
using System.Linq;

namespace Xpand.Extensions.LinqExtensions {
    public static partial class LinqExtensions {
        public static IReadOnlyList<T> MoveFirstToEnd<T>(this IReadOnlyList<T> source) {
            if (source is not { Count: > 1 }) return source;
            var list = source.ToList();
            var firstItem = list[0];
            list.RemoveAt(0);
            list.Add(firstItem);
            return list;
        }
    }
}