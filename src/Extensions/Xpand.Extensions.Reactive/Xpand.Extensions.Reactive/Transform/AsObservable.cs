using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Transform{
    public static partial class Transform{

        public static IObservable<T> AsObservable<T>(this T self, IScheduler scheduler = null){
            scheduler = scheduler ?? Scheduler.Immediate;
            return Observable.Return(self, scheduler);
        }
    }
}