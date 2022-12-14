using System;
using System.Collections.Generic;

namespace Xpand.Extensions.LinqExtensions {
    public static partial class LinqExtensions {
        public static IEnumerable<T> TryDo<T>(this IEnumerable<T> source, Action<T> tryDo) => source.Execute(obj => {
            try {
                tryDo(obj);
            }
            catch {
                // ignored
            }
        });
    }
}