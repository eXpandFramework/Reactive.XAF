using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using Xpand.Extensions.Numeric;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Transform.System;

namespace Xpand.Extensions.Reactive.Utility {
    public static partial class Utility {
        public static IObservable<T> DelayOnContext<T>(this IObservable<T> source,int seconds=1,bool delayOnEmpty=false) 
            => source.DelayOnContext(seconds.Seconds(),delayOnEmpty);
        public static IObservable<T> DelayOnContext<T>(this IObservable<T> source,TimeSpan? timeSpan,bool delayOnEmpty=false) 
            => source.If(_ => timeSpan.HasValue,arg => arg.DelayOnContext( (TimeSpan)timeSpan!),arg => arg.Observe())
                .SwitchIfEmpty(timeSpan.Observe().Where(_ => delayOnEmpty).WhenNotDefault().SelectMany(span => span.DelayOnContext((TimeSpan)span!)
                    .Select(_ => default(T)).IgnoreElements()));

        private static IObservable<T> DelayOnContext<T>(this T arg,TimeSpan timeSpan) 
            => arg.Observe().SelectManySequential( arg1 => timeSpan.Timer(new SynchronizationContextScheduler(SynchronizationContext.Current!)).ObserveOnContext().To(arg1));
        public static IObservable<T> Defer<T>(this object o, IObservable<T> execute)
            => Observable.Defer(() => execute);
        
        public static IObservable<Unit> DeferAction<T>(this T o, Action execute)
            => Observable.Defer(() => {
                execute();
                return Observable.Empty<Unit>();
            });
        public static IObservable<Unit> DeferAction<T>(this T o, Action<T> execute)
            => Observable.Defer(() => {
                execute(o);
                return Observable.Empty<Unit>();
            });

        public static IObservable<T> Defer<T,TObject>(this TObject o, Func<TObject,IObservable<T>> selector)
            => Observable.Defer(() => selector(o));
        
        public static IObservable<T> Defer<T>(this object o, Func<IObservable<T>> selector)
            => Observable.Defer(selector);
        
        public static IObservable<T> Defer<T>(this object o, Func<IEnumerable<T>> selector)
            => Observable.Defer(() => selector().ToNowObservable());
        
        // public static IObservable<T> DelaySubscription<T>(this IObservable<T> source, TimeSpan delay, IScheduler scheduler = null) 
        //     => scheduler == null ? Observable.Timer(delay).SelectMany(_ => source) : Observable.Timer(delay, scheduler).SelectMany(_ => source);

        public static IObservable<T> DelayRandomly<T>(this IObservable<T> source, int maxValue, int minValue = 0)
            => source.SelectMany(arg => {
                var value = Random.Next(minValue, maxValue);
                return value == 0 ? arg.Observe() : Observable.Timer(TimeSpan.FromSeconds(value)).To(arg);
            });
    }
}