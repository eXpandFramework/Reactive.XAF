using System;
using System.Collections.Generic;

namespace Xpand.Extensions.LinqExtensions {
    public static partial class LinqExtensions {
        public static IEnumerable<TSource> StartWith<TSource>(this IEnumerable<TSource> source, Func<bool> when, Func<TSource[]> values) 
            => when() ? values() : source;
    }
}