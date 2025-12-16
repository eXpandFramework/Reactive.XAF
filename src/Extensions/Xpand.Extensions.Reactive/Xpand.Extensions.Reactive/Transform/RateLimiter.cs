using System;
using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Xpand.Extensions.Reactive.Transform {
    public static partial class Transform {
        private static readonly ConcurrentDictionary<string, RateLimitingGroup> Groups = new();

        public static IObservable<T> RateLimit<T>(this IObservable<T> source, double emissionsPerSecond,
            string groupName = nameof(RateLimit))
            => emissionsPerSecond <= 0 ? source : Observable.Defer(() => {
                    var group = Groups.GetOrAdd(groupName, static (_, rate) => new RateLimitingGroup(rate), emissionsPerSecond);
                    return source.Select(item => Observable.FromAsync(group.WaitAsync).Select(_ => item)
                    ).Concat();
                });        
        
        public static IObservable<T> RateLimit<T>(this T source, double emissionsPerSecond, string groupName = nameof(RateLimit))
            =>source.Observe().RateLimit(emissionsPerSecond, groupName);
        
        private class RateLimitingGroup(double maxRequestsPerSecond) {
            private readonly SemaphoreSlim _semaphore = new(1, 1);
            private DateTimeOffset _lastEmission = DateTimeOffset.MinValue;

            public async Task WaitAsync() {
                await _semaphore.WaitAsync();
                try {
                    var now = DateTimeOffset.UtcNow;
                    var requiredDelay = _lastEmission + TimeSpan.FromSeconds(1.0 / maxRequestsPerSecond) - now;
                    if (requiredDelay > TimeSpan.Zero) {
                        await Task.Delay(requiredDelay, CancellationToken.None);
                    }
                    _lastEmission = DateTimeOffset.UtcNow;
                }
                finally {
                    _semaphore.Release();
                }
            }
        }
        
    }
    }
    
