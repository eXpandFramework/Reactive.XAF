using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Xpand.Extensions.LinqExtensions {
    public static partial class LinqExtensions {
        public static ConcurrentDictionary<TKey, TElement> ToConcurrentDictionary<TSource, TKey, TElement>(
            this IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector) where TKey : notnull {
            var concurrentDictionary = new ConcurrentDictionary<TKey, TElement>();
            foreach (var item in source.ToArray()) {
                concurrentDictionary.TryAdd(keySelector(item), elementSelector(item));
            }

            return concurrentDictionary;
        }
    }
}