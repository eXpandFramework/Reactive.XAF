using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Xpand.Extensions.ObjectExtensions {
    public static partial class ObjectExtensions {
        public static TResource With<TResource>(this object key)
            where TResource : IDisposable, new()
            => ScopedCache<TResource>.GetOrAdd(key, static () => new());

        public static TResource With<TResource>(this object key, Func<TResource> factory)
            where TResource : IDisposable
            => ScopedCache<TResource>.GetOrAdd(key, factory);

        static class ScopedCache<TResource> where TResource : IDisposable {
            static readonly ConditionalWeakTable<object, Scope> Cache = new();

            public static TResource GetOrAdd(object key, Func<TResource> factory)
                => Cache.GetValue(key, _ => new Scope(factory()))!.Resource;

            sealed class Scope(TResource resource) : IDisposable {
                public TResource Resource { get; } = resource;
                int _disposed;
                public void Dispose(){
                    if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
                    Resource.Dispose();
                }
                ~Scope() => Dispose();
            }
        }
    }
}