using System.Collections.Generic;
using Swordfish.NET.Collections.Auxiliary;

namespace Xpand.Extensions.LinqExtensions {
    public static partial class LinqExtensions {
        public static void Subscribe<T>(this IEnumerable<T> list)
            => list.ForEach(_ => { });
    }
}