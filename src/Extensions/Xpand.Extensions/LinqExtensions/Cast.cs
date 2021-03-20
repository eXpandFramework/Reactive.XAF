using System.Collections;
using System.Collections.Generic;

namespace Xpand.Extensions.LinqExtensions {
    public static partial class LinqExtensions {
        public static IEnumerator<T> Cast<T>(this IEnumerator iterator) {
            while (iterator.MoveNext()) {
                yield return (T) iterator.Current;
            }
        }
    }
}