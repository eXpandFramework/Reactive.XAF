using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Xpand.Extensions.Reactive.Combine;

namespace Xpand.Extensions.Reactive.Transform{
    public static partial class Transform {
        public static IObservable<T> SampleResponsive<T>(
            this IObservable<T> source, TimeSpan delay, IScheduler scheduler = null) {
            scheduler ??= Scheduler.Default;
            return source.Publish(src => {
                var fire = new Subject<T>();

                var whenCanFire = fire
                    .Select(_ => new Unit())
                    .Delay(delay, scheduler)
                    .StartWith(scheduler, new Unit());

                var subscription = src
                    .CombineVeryLatest(whenCanFire, (x, _) => x)
                    .Subscribe(fire);

                return fire.Finally(subscription.Dispose);
            });
        }
    }
}