using System.Collections.Generic;

namespace Xpand.Extensions.LinqExtensions;

public static partial class LinqExtensions {
    public static void Swap<T>(this IList<T> list, int i, int j) => (list[i], list[j]) = (list[j], list[i]);
}