using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Transform{
    public static partial class Transform {
        public static IObservable<T[]> RollingBuffer<T>(this IObservable<T> source, TimeSpan buffering, IScheduler scheduler = null) {
            scheduler ??= TaskPoolScheduler.Default;
            return Observable.Create<T[]>(o => {
                var list = new LinkedList<Timestamped<T>>();
                return source.Timestamp(scheduler).Subscribe(tx => {
                    list.AddLast(tx);
                    while (scheduler.Now.Ticks > buffering.Ticks &&
                           (list.First.Value.Timestamp < scheduler.Now.Subtract(buffering)))
                        list.RemoveFirst();
                    o.OnNext(list.Select(tx2 => tx2.Value).ToArray());
                }, o.OnError, o.OnCompleted);
            });
        }
    }
}