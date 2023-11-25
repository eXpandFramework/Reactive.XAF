using System;
using System.Reactive;
using System.Reactive.Linq;
using Xpand.Extensions.Reactive.Filter;

namespace Xpand.Extensions.Reactive.Transform {
    public static partial class Transform {
        public static IObservable<T> ConcatDefer<T>(this IObservable<T> source, Func<IObservable<T>> target)
            => source.Concat(Observable.Defer(target));

        public static IObservable<Unit> ConcatDeferToUnit<T>(this IObservable<T> source, Func<IObservable<T>> target)
            => source.ToUnit().Concat(Observable.Defer(target).ToUnit());
        public static IObservable<Unit> ConcatDeferToUnit<T>(this IObservable<T> source, Func<IObservable<object>> target)
            => source.ToUnit().Concat(Observable.Defer(target).ToUnit());
        
        public static IObservable<Unit> ConcatToUnit<T,T1>(this IObservable<T> source, IObservable<T1> target)
            => source.ToUnit().Concat(Observable.Defer(target.ToUnit));
        
        public static IObservable<object> ConcatToObject<T,T1>(this IObservable<T> source, IObservable<T1> target)
            => source.ToObject().Concat(Observable.Defer(target.ToObject));
        
        public static IObservable<TTarget> ConcatIgnoredValue<TSource,TTarget>(this IObservable<TSource> source, TTarget value) 
            => source.Select(_ => default(TTarget)).WhenNotDefault().Concat(value.Observe());
        
        public static IObservable<Unit> ConcatIgnoredUnit<TSource>(this IObservable<TSource> source) 
            => source.ConcatIgnoredValue(Unit.Default);
        public static IObservable<T> ConcatIgnored<T,T2>(this IObservable<T> source,Func<T,IObservable<T2>> secondSelector,Func<T,bool> merge=null)
            => source.SelectMany(arg => {
                merge ??= _ => true;
                return merge(arg) ? secondSelector(arg).IgnoreElements().ConcatIgnoredValue(arg).Finally(() => {}) : arg.Observe();
            });
        public static IObservable<T> ConcatIgnored<T, T2>(this IObservable<T> source, IObservable<T2> secondSelector)
            => source.ConcatIgnored(_ => secondSelector);
        public static IObservable<T> ConcatIgnored<T>(this IObservable<T> source,Action<T> action,Func<T,bool> merge=null)
            => source.SelectMany(arg => {
                merge ??= _ => true;
                if (merge(arg)) {
                    action(arg);
                    return Observable.Empty<T>().ConcatIgnoredValue(arg);
                }
                return arg.Observe();
            });
        public static IObservable<T> ConcatIgnoredFirst<T,T2>(this IObservable<T> source,Func<T,IObservable<T2>> secondSelector,Func<T,bool> merge=null)
            => source.SelectMany((arg, i) => {
                var observable = Observable.Empty<T>();
                if (i == 0) {
                    merge ??= _ => true;
                    if (merge(arg)) {
                        observable = secondSelector(arg).IgnoreElements().To(arg);
                    }
                    return observable.Concat(arg.Observe());
                }

                return arg.Observe();

            });
    }
}