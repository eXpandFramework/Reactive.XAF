using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Xpand.Extensions.ObjectExtensions;


namespace Xpand.Extensions.Reactive.Transform {
    public partial class Transform {
        [Obsolete]
        public static RxChannelProvider<TKey> When<TKey>(this TKey key) => new(key);
        [Obsolete]
        public class RxChannelProvider<TKey>(TKey key) {
            [Obsolete]
            public IObservable<TValue> Response<TValue>()
                => key.With(() => new RXChannel<TKey, TValue>()).WhenResponse(key);
            [Obsolete]
            public IObservable<TValue> Request<TValue>(IObservable<TValue> responseSource)
                => key.With(() => new RXChannel<TKey, TValue>()).WhenRequest(key, responseSource);
        }
        sealed class RXChannel<TKey, TValue> : IDisposable where TKey : notnull {
            readonly Subject<(TKey Key, Guid RequestId)> _requests = new();
            readonly ConcurrentDictionary<(TKey Key, Guid RequestId), AsyncSubject<TValue>> _pending = new();
            bool _disposed;
            [Obsolete]
            public IObservable<TValue> WhenResponse(TKey key, TimeSpan? timeout = null) {
                var requestId = Guid.NewGuid();
                var sink = new AsyncSubject<TValue>();
                _pending[(key, requestId)] = sink;
                _requests.OnNext((key, requestId));

                var source = sink.AsObservable();
                if (timeout.HasValue)
                    source = source.Timeout(timeout.Value);

                return Observable.Using(
                    () => Disposable.Create((_pending, key, requestId), static s => {
                        var tryRemove = s.Item1.TryRemove((s.Item2, s.Item3), out _);
                        if (tryRemove) {
                            
                        }
                    }),
                    _ => source.Select(value => value)
                );
            
            }
            [Obsolete]
            public IObservable<TValue> WhenRequest(TKey key, IObservable<TValue> responseSource) =>
                _requests.Where(t => EqualityComparer<TKey>.Default.Equals(t.Key, key))
                    .SelectMany(t =>                 // t contains (key, requestId)
                        responseSource.Take(1).Do(
                            v => {
                                if (_pending.TryRemove((t.Key, t.RequestId), out var sink)) {
                                    sink.OnNext(v);
                                    sink.OnCompleted();
                                }
                            },
                            e => {
                                if (_pending.TryRemove((t.Key, t.RequestId), out var sink))
                                    sink.OnError(e);
                            }));

            public void Dispose() {
                if (_disposed) return;
                _disposed = true;
                _requests.OnCompleted();
                foreach (var sink in _pending.Values)
                    sink.OnCompleted();
                _requests.Dispose();
            }
        }

    }
}
