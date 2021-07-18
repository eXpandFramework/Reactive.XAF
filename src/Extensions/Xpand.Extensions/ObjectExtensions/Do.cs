using System;
using System.Collections.Generic;
using System.Linq;

namespace Xpand.Extensions.ObjectExtensions {
    public static partial class ObjectExtensions {
        public static IEnumerable<TSource> Do<TSource>(
            this IEnumerable<TSource> source, Action<TSource> selector, Action<Exception> error = null)
            => source.Select(source1 => {
                try {
                    selector(source1);

                }
                catch (Exception e) {
                    error?.Invoke(e);
                    throw;
                }

                return source1;
            });
    }
}