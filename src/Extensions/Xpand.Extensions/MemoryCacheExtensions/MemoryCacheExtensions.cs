using System;
using Humanizer;
using Microsoft.Extensions.Caching.Memory;

namespace Xpand.Extensions.MemoryCacheExtensions;
public static class MemoryCacheExtensions {
    public static bool TryAdd<TKey>(this IMemoryCache cache, TKey key, TimeSpan? ttl = null) {
        bool wasAdded = false;
        cache.GetOrCreate(key, entry => {
            wasAdded = true;
            entry.SetAbsoluteExpiration(ttl ?? 30.Minutes());
            entry.SetSize(1);
            return new object();
        });
        
        return wasAdded;
    }
}