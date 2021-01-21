using System;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Utility {
    public static partial class Utility {
        public static IObservable<T> DoOnPrevious<T>(this IObservable<T> source, Action<T> onPrevious)
            => source.Select(x => (Item: x, HasValue: true))
                .Append((default, false))
                .Scan((previous, current) => {
                    if (previous.HasValue) onPrevious(previous.Item);
                    return current;
                })
                .Where(entry => entry.HasValue)
                .Select(entry => entry.Item);
    }

}