using System;
using System.Diagnostics;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;

namespace Xpand.Extensions.Reactive.Utility {
    public static partial class Utility {
        public static IObservable<TSource> DroppingDo<TSource>(this IObservable<TSource> source,
            Action<TSource> action, IScheduler scheduler = null) {
            scheduler ??= Scheduler.Default;
            return Observable.Defer(() => {
                Tuple<TSource> latest = null;
                return source
                    .Select(item => {
                        var previous = Interlocked.Exchange(ref latest, Tuple.Create(item));
                        if (previous != null) return Observable.Empty<TSource>();
                        return Observable.Defer(() => {
                            var current = Interlocked.Exchange(ref latest, null);
                            Debug.Assert(current != null);
                            var unBoxed = current.Item1;
                            return Observable.Start(() => {
                                    action(unBoxed);
                                    return unBoxed;
                                }, scheduler);
                        });
                    })
                    .Concat();
            });
        }
    }
}