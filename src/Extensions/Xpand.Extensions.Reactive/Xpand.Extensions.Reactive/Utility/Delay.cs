using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using Xpand.Extensions.Numeric;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.Extensions.Reactive.ErrorHandling;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.Extensions.Reactive.Utility {
    public static partial class Utility {
        public static IObservable<T> DelayOnContext<T>(this IObservable<T> source,int seconds=1,bool delayOnEmpty=false) 
            => source.DelayOnContext(seconds.Seconds(),delayOnEmpty);
        public static IObservable<T> DelayOnContext<T>(this IObservable<T> source,TimeSpan? timeSpan,bool delayOnEmpty=false) 
            => source.If(_ => timeSpan.HasValue,arg => arg.DelayOnContext( (TimeSpan)timeSpan!),arg => arg.Observe())
                .SwitchIfEmpty(timeSpan.Observe().Where(_ => delayOnEmpty).WhenNotDefault().SelectMany(span => span.DelayOnContext((TimeSpan)span!)
                    .Select(_ => default(T)).IgnoreElements()));

        private static IObservable<T> DelayOnContext<T>(this T arg,TimeSpan timeSpan) 
            => arg.Observe()
                .SelectManySequential( arg1 => Observable.Return(arg1).Delay(timeSpan).ObserveOnContext());

        public static IObservable<T> Defer<T>(this object o, IObservable<T> execute)
            => o.Defer(() => execute);
        
        public static IObservable<Unit> DeferAction<T>(this T o, Action execute)
            => o.Defer(() => {
                execute();
                return Observable.Empty<Unit>();
            });
        public static IObservable<Unit> DeferAction<T>(this T o, Action<T> execute)
            => o.Defer(() => {
                execute(o);
                return Observable.Empty<Unit>();
            });

        public static IObservable<T> Defer<T,TObject>(this TObject o, Func<TObject,IObservable<T>> selector)
            => o.Defer(() => selector(o));
        
        public static IObservable<T> Defer<T>(this object o, Func<IObservable<T>> selector) 
            => Observable.Defer(selector);

        public static IObservable<T> Defer<T>(this object o, Func<IEnumerable<T>> selector)
            => o.Defer(() => selector().ToNowObservable());
        
        public static IObservable<T> DelayUntil<T, TSignal>(this IObservable<T> source, IObservable<TSignal> trigger) 
            => source.Select(x => trigger.Take(1).Select(_ => x)).Concat();
        public static IObservable<T> DelayUntilSequential<T, TSignal>(this IObservable<T> source, Func<T, IObservable<TSignal>> triggerSelector) =>
            source.Select(x => triggerSelector(x).Take(1).Select(_ => x)).Concat();

        public static IObservable<T> DelayUntil<T, TSignal>(this IObservable<T> source, Func<T, IObservable<TSignal>> triggerSelector) =>
            source.SelectMany(x => triggerSelector(x).Take(1).Select(_ => x));

        public static IObservable<T> DelayRandomly<T>(this IObservable<T> source, int maxValue, int minValue = 0)
            => source.SelectMany(arg => {
                var value = Random.Next(minValue, maxValue);
                return value == 0 ? arg.Observe() : Observable.Timer(TimeSpan.FromSeconds(value)).To(arg);
            });
    }
}