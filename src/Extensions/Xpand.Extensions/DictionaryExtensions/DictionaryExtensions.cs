using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Numeric;

namespace Xpand.Extensions.DictionaryExtensions {
    public static class DictionaryExtensions {
        public static TValue TryUpdate<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dict, TKey key, Func<TValue, TValue> updateFactory) {
            while(dict.TryGetValue(key, out var curValue)) {
                if(dict.TryUpdate(key, updateFactory(curValue), curValue))
                    return curValue;
            }
            return default;
        }

        public static TValue Update<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dict, TKey key, Func<TValue, TValue> updateFactory)
            => dict.TryUpdate(key, updateFactory);

        public static TValue Update<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dict, TKey key, Action<TValue> updateFactory)
            => dict.TryUpdate(key, value => {
                updateFactory(value);
                return value;
            });
		
        public static bool TryGetValue<T>(this IList<T> array, int index, out T value){
            if (array.IsValidIndex( index)){
                value = array[index];
                return true;
            }
            value = default;
            return false;
        }
        
        public static bool AddWithCap<TKey,TValue>(this ConcurrentDictionary<TKey, TValue> cache, TKey id, int cap)
            => cache.Count > cap
                ? cache.TryUpdate(id, default, cache[id]) 
                : cache.TryAdd(id, default);

        public static void ClearWhenFull<TKey,TValue>(this ConcurrentDictionary<TKey, TValue> cache, int cap) {
            if (cache.Count > cap) cache.Clear();
        }

        public static void AddWithTtl<TKey,TValue>(this ConcurrentDictionary<TKey, TValue> cache, TKey id, TimeSpan ttl) {
            if (!cache.TryAdd(id, default)) return;          
            _ = RemoveLater(id, ttl, cache);           
        }

        static async Task RemoveLater<TKey,TValue>(TKey id, TimeSpan ttl, ConcurrentDictionary<TKey, TValue> cache) {
            try { await Task.Delay(ttl).ConfigureAwait(false); }
            catch (TaskCanceledException) { return; }  
            cache.TryRemove(id, out _);
        }
        
        public static bool AddWithTtlAndCap<TKey,TValue>(this ConcurrentDictionary<TKey, TValue> cache, TKey key,TValue value, TimeSpan ttl, int cap) {
            if (cache.Count > cap)
                cache.Clear();
            if (!cache.TryAdd(key, value))
                return false;                          
            _ = Task.Delay(ttl).ContinueWith(_ => cache.TryRemove(key, out var _),
                    TaskScheduler.Default);
            return true;                               
        }
        public static bool AddWithTtlAndCap<TKey>(this ConcurrentDictionary<TKey, byte> cache, TKey key,TimeSpan? ttl=null, int cap=10000) {
            if (cache.Count > cap)
                cache.Clear();
            if (!cache.TryAdd(key, 0))
                return false;                          
            _ = Task.Delay(ttl??5.ToMinutes()).ContinueWith(_ => cache.TryRemove(key, out var _),
                    TaskScheduler.Default);
            return true;                               
        }
    }
}