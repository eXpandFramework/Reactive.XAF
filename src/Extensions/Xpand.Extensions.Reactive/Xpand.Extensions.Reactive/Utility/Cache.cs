using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Xpand.Extensions.Reactive.Utility {
    public static partial class Utility {
        public static IObservable<T2> Cache<T, T2>(this IObservable<T> source,
            ConcurrentDictionary<object, IConnectableObservable<object>> storage, object key,
            Func<T, IObservable<T2>> secondSelector, TimeSpan? interval) where T2 : class
            => source.SelectMany(message => {
                    if (interval.HasValue) {
                        if (storage.TryGetValue(key, out var value)) {
                            return value.Select(o => o).FirstAsync().Cast<T2>().Finally(() => { });
                        }
                        var publish = Observable.Interval(interval.Value).StartWith(0)
                            .SelectMany(_ => secondSelector(message))
                            .Publish().RefCount().Replay(1);
                        publish.Connect();
                        storage.TryAdd(key, publish);
                        return publish.Select(o => o).FirstAsync().Cast<T2>().Finally(() => { });
                    }

                    return secondSelector(message);
                });
    }
}