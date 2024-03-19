using System;
using System.Collections.Concurrent;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Reactive.Transform {
    public static partial class Transform {
        private static readonly ConcurrentDictionary<string, RateLimitingGroup> Groups = new();

        public static IObservable<T> RateLimit<T>(this T source, double emissionsPerSecond, string groupName = nameof(RateLimit))
            =>emissionsPerSecond==0?source.Observe(): source.Defer(() => {
                var group = Groups.GetOrAdd(groupName, new RateLimitingGroup(emissionsPerSecond));
                return Observable.StartAsync(() => group.WaitAsync()).Select(_ => source);
            });
        
        private class RateLimitingGroup {
            private readonly SemaphoreSlim _semaphore;
            private readonly double _maxRequestsPerSecond;
            
            private DateTimeOffset _lastEmission;
    
            public RateLimitingGroup(double maxRequestsPerSecond) {
                _maxRequestsPerSecond = maxRequestsPerSecond;
                
                _semaphore = new SemaphoreSlim(1, 1);
                _lastEmission = DateTimeOffset.MinValue;
            }
    
            public async Task WaitAsync() {
                await _semaphore.WaitAsync();
                var now = DefaultScheduler.Instance.Now;
                var requiredDelay = _lastEmission + TimeSpan.FromSeconds(1.0 / _maxRequestsPerSecond) - now;
                if (requiredDelay > TimeSpan.Zero) {
                    await Task.Delay(requiredDelay, CancellationToken.None);
                }
    
                _lastEmission = DefaultScheduler.Instance.Now;
                _semaphore.Release();
            }
        }
        }
    }
    
