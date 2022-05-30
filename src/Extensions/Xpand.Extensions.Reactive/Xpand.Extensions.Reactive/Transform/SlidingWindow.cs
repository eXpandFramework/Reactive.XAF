using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Transform {
    public static partial class Transform {
        public static IObservable<IReadOnlyList<Timestamped<T>>> SlidingWindow<T>(this IObservable<Timestamped<T>> source, TimeSpan length) 
            => source.Scan(new LinkedList<Timestamped<T>>(), (list, newSample) => {
                    list.AddLast(newSample);
                    var oldest = newSample.Timestamp - length;
                    while (list.Count > 0 && list.First.Value.Timestamp < oldest)
                        list.RemoveFirst();

                    return list;
                }).Select(l => l.ToList().AsReadOnly());
    }
}