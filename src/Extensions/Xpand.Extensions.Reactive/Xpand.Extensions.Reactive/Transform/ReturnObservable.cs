using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Reactive.Transform{
    public static partial class Transform{
        
        public static ResilientObservable<T> Observe<T>(this T self, IScheduler scheduler = null) 
            => Observable.Return(self, scheduler ?? ImmediateScheduler).ToResilientObservable();
        
        public static ResilientObservable<T> Observe<T,T2>(this T self,Func<T,IObservable<T2>> secondSelector, IScheduler scheduler = null) 
            => self.Observe(scheduler).MergeIgnored(secondSelector);
        
        public static ResilientObservable<T> ObserveOnWindows<T>(this ResilientObservable<T> source, SynchronizationContext synchronizationContext)
            => source.AsObservable().ObserveOnWindows(synchronizationContext).ToResilientObservable();

        public static ResilientObservable<T> ObserveOnDefault<T>(this ResilientObservable<T> source)
            => source.AsObservable().ObserveOnDefault().ToResilientObservable();

        public static ResilientObservable<T> ObserveOnCurrent<T>(this ResilientObservable<T> source)
            => source.AsObservable().ObserveOnCurrent().ToResilientObservable();

        public static ResilientObservable<T> ObserveOnContext<T>(this ResilientObservable<T> source, SynchronizationContext context = null)
            => source.AsObservable().ObserveOnContext(context).ToResilientObservable();

        public static ResilientObservable<T> ObserveOnContextMaybe<T>(this ResilientObservable<T> source)
            => source.AsObservable().ObserveOnContextMaybe().ToResilientObservable();

        public static ResilientObservable<T> ObserveOn<T>(this ResilientObservable<T> source, SynchronizationContext synchronizationContext, Func<bool> state)
            => source.AsObservable().ObserveOn(synchronizationContext, state).ToResilientObservable();

        public static ResilientObservable<TSource> ObserveLatestOn<TSource>(this ResilientObservable<TSource> source, SynchronizationContext context)
            => source.AsObservable().ObserveLatestOn(context).ToResilientObservable();

        public static ResilientObservable<TSource> ObserveLatestOnContext<TSource>(this ResilientObservable<TSource> source)
            => source.AsObservable().ObserveLatestOnContext().ToResilientObservable();

        public static ResilientObservable<TSource> ObserveLatest<TSource>(this ResilientObservable<TSource> source)
            => source.AsObservable().ObserveLatest().ToResilientObservable();

        public static ResilientObservable<T> ObserveLatestOn<T>(this ResilientObservable<T> source, IScheduler scheduler)
            => source.AsObservable().ObserveLatestOn(scheduler).ToResilientObservable();
        
        
    }
}