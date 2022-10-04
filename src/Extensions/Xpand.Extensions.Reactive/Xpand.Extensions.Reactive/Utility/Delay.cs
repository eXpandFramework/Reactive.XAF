using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.Extensions.Reactive.Utility {
    public static partial class Utility {
        public static IObservable<T> Defer<T>(this object o, IObservable<T> execute)
            => Observable.Defer(() => execute);
        public static IObservable<T> Defer<T>(this object o, Func<IObservable<T>> selector)
            => Observable.Defer(selector);
        public static IObservable<T> Defer<T>(this object o, Func<IEnumerable<T>> selector)
            => Observable.Defer(() => selector().ToNowObservable());

        public static IObservable<T> Defer<T>(this object o, Action execute)
            => Observable.Defer(() => {
                execute();
                return o.ReturnObservable();
            }).To<T>();
        public static IObservable<T> Defer<T>(this T o, Action<T> execute)
            => Observable.Defer(() => {
                execute(o);
                return o.ReturnObservable();
            });
        
        public static IObservable<Unit> Defer(this object o, Action execute)
            => Observable.Defer(() => {
                execute();
                return Unit.Default.ReturnObservable();
            });
        
        public static IObservable<Unit> Defer(this object o,TimeSpan timeSpan, Action execute)
            => Unit.Default.ReturnObservable().Delay(timeSpan).Do(execute).ToUnit();
        
        
        public static IObservable<T> DelaySubscription<T>(this IObservable<T> source, TimeSpan delay, IScheduler scheduler = null) 
            => scheduler == null ? Observable.Timer(delay).SelectMany(_ => source) : Observable.Timer(delay, scheduler).SelectMany(_ => source);

        public static IObservable<T> DelayRandomly<T>(this IObservable<T> source, int maxValue, int minValue = 0)
            => source.SelectMany(arg => {
                var value = Random.Next(minValue, maxValue);
                return value == 0 ? arg.ReturnObservable() : Observable.Timer(TimeSpan.FromSeconds(value)).To(arg);
            });
    }
}