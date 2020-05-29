using System;
using System.Collections.Generic;

namespace Xpand.Extensions.LinqExtensions{
    public static partial class LinqExtensions{
        public static IEnumerable<TSource> FromHierarchy<TSource>(this TSource source, Func<TSource, TSource> nextItem) where TSource : class => FromHierarchy(source, nextItem, s => s != null);

        public static IEnumerable<TSource> FromHierarchy<TSource>(this TSource source, Func<TSource, TSource> nextItem, Func<TSource, bool> canContinue){
            for (var current = source; canContinue(current); current = nextItem(current)) yield return current;
        }
    }
}