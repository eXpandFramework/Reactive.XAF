using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;

namespace Xpand.Extensions.Reactive.Transform{
    public static partial class Transform{
        public static IObservable<TSource> ToObservable<TSource>(this IEnumerable<TSource> source, SynchronizationContext context)
            => source.ToObservable().ObserveOn(context);
        public static IObservable<TSource> ToNowObservable<TSource>(this IEnumerable<TSource> source)
            => source.ToObservable(ImmediateScheduler);
        
        public static IObservable<TSource> Consume<TSource>(this BlockingCollection<TSource> source)
            => source.GetConsumingEnumerable().ToObservable(global::System.Reactive.Concurrency.Scheduler.Default);
    }
}