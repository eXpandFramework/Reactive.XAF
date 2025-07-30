using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;

namespace Xpand.Extensions.Reactive.Transform{
    public static partial class Transform{
        public static ResilientObservable<TSource> ToObservable<TSource>(this IEnumerable<TSource> source, SynchronizationContext context)
            => source.ToObservable().ObserveOn(context).ToResilientObservable();
        
        public static ResilientObservable<TSource> ToNowObservable<TSource>(this IEnumerable<TSource> source)
            => source.ToObservable(ImmediateScheduler).ToResilientObservable();
        
        public static ResilientObservable<TSource> Consume<TSource>(this BlockingCollection<TSource> source)
            => source.GetConsumingEnumerable().ToObservable(Scheduler.Default).ToResilientObservable();
        
        
    }
}