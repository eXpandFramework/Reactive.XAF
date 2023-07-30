using System.Collections.Generic;

namespace Xpand.Extensions.LinqExtensions {
    public static partial class LinqExtensions {
        public static void Enumerate<T>(this IEnumerable<T> source) {
            using var e = source.GetEnumerator();
            while (e.MoveNext()) { }
        }
    }
}