using System;
using System.Collections.Concurrent;
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
        
        public static ConcurrentDictionary<TKey, TElement> ToConcurrentDictionary<TSource, TKey, TElement>(
            this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector) where TKey : notnull 
            => source.ToConcurrentDictionary(keySelector, elementSelector, EqualityComparer<TKey>.Default);

        public static ConcurrentDictionary<TKey, TElement> ToConcurrentDictionary<TSource, TKey, TElement>(
            this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector,IEqualityComparer<TKey> comparer) where TKey : notnull {
            var concurrentDictionary = new ConcurrentDictionary<TKey, TElement>(comparer);
            foreach (var item in source.ToArray()) {
                concurrentDictionary.TryAdd(keySelector(item), elementSelector(item));
            }
            return concurrentDictionary;
        }
        
        public static ConcurrentDictionary<TKey, TLink> ToConcurrentDictionary<TKey,TLink>(this IEnumerable<TLink> source,Func<TLink,TKey> key)  
            => source.ToConcurrentDictionary(key, link => link);
        
        public static ConcurrentDictionary<TKey, TLink> ToConcurrentDictionary<TKey,TLink>(this IEnumerable<TLink> source,Func<TLink,TKey> key,IEqualityComparer<TKey> comparer)  
            => source.ToConcurrentDictionary(key, link => link,comparer);
        
        public static Dictionary<TKey, TLink> ToDictionary<TKey,TLink>(this IEnumerable<TLink> source,Func<TLink,TKey> key) 
            => source.ToDictionary(key, link => link);
        
        public static Dictionary<TKey, TLink> ToDictionary<TKey,TLink>(this IEnumerable<TLink> source,Func<TLink,TKey> key,IEqualityComparer<TKey> comparer) 
            => source.ToDictionary(key, link => link,comparer);
    }
}