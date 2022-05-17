using System;
using System.Collections.Generic;
using System.Linq;

namespace Xpand.Extensions.LinqExtensions {
    public static partial class LinqExtensions {
        public static Dictionary<TKey, TValue> ToOrderedDictionary<TKey, TValue>(
            this IEnumerable<(TKey key, TValue value)> source, Func<TKey, TValue, TValue> orderBySelector)
            => source.OrderBy(t => orderBySelector(t.key, t.value)).ToDictionary(t => t.key, t => t.value);
        
        public static Dictionary<TKey, TValue> ToOrderedDictionary<TKey, TValue>(this IEnumerable<(TKey key, TValue value)> source)
            => source.OrderBy(t => t.key).ToDictionary(t => t.key, t => t.value);
        
        public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IEnumerable<(TKey key, TValue value)> source) 
            => source.ToDictionary(t => t.key, t => t.value);
    }
}