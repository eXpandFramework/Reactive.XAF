using System;
using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Xpand.Extensions.Reactive.Utility {
    public static partial class Utility {
        
        public static IObservable<T2> Cache<T, T2,TKey>(this IObservable<T> source,
            ConcurrentDictionary<TKey, IConnectableObservable<object>> storage, TKey key,
            Func<T, IObservable<T2>> secondSelector, TimeSpan? interval) where T2 : class
            => source.SelectMany(message => {
                    if (interval.HasValue) {
                        if (storage.TryGetValue(key, out var value)) {
                            return value.FirstAsync().Cast<T2>();
                        }
                        var publish = Observable.Interval(interval.Value)
                            .SelectMany(_ => secondSelector(message))
                            .Publish().RefCount().Replay(1);
                        var connection = publish.Connect();
                        storage.TryAdd(key, publish);
                        return publish.FirstAsync().Finally(() =>connection.Dispose() ).Cast<T2>();
                    }

                    return secondSelector(message);
                });
    }
}