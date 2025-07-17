using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Transform.System.Text.Json;

namespace Xpand.Extensions.Reactive.Utility {
    public static partial class Utility {
        
        public static IObservable<T2> Cache<T, T2,TKey>(this IObservable<T> source,
            ConcurrentDictionary<TKey, IConnectableObservable<object>> storage, TKey key,
            Func<T, IObservable<T2>> secondSelector, TimeSpan? interval) where T2 : class
        {
            if (interval.HasValue) {
                if (!storage.TryGetValue(key, out var value)) {
                    value = source.Take(1).SelectMany(message => Observable.Timer(TimeSpan.Zero, interval.Value)
                            .Take(1)
                            .SelectMany(_ => secondSelector(message)))
                        .Publish().RefCount().Replay(1);
                    var connection = value.Connect();
                    storage.TryAdd(key, value);
                    return value.Finally(() => connection.Dispose()).Cast<T2>();
                }
                return value.Take(1).Cast<T2>();
            }

            return source.SelectMany(secondSelector);
        }


        public static IObservable<Dictionary<TKey, TValue>> Cache<T, TKey, TValue>(this IObservable<T[]> source,
            Func<T, TKey> keySelector, Func<T, TValue> valueSelector) {
            var map = new Dictionary<TKey, TValue>();
            return source.SelectMany(filters => filters.ToNowObservable()
                    .Do(obj => map[keySelector(obj)] = valueSelector(obj)).TakeLast(1)
                    .Select(_ => map))
                .Finally(() => map.Clear());
        }
    }
}