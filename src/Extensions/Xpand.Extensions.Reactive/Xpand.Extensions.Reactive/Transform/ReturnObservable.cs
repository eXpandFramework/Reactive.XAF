using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Xpand.Extensions.Reactive.Combine;

namespace Xpand.Extensions.Reactive.Transform{
    public static partial class Transform{
        public static IObservable<T> ReturnObservable<T>(this T self, IScheduler scheduler = null) 
            => Observable.Return(self, scheduler ?? ImmediateScheduler);

        public static IObservable<T> ReturnObservable<T,T2>(this T self,Func<T,IObservable<T2>> secondSelector, IScheduler scheduler = null) 
            => self.ReturnObservable().MergeIgnored(secondSelector);
    }
}