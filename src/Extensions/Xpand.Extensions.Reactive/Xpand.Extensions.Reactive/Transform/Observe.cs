using System;
using System.Reactive.Linq;
using Xpand.Extensions.Reactive.Combine;

namespace Xpand.Extensions.Reactive.Transform {
    public static partial class Transform {
        public static IObservable<T> MergeIgnored<T,T2>(this IObservable<T> source,Func<T,IObservable<T2>> secondSelector,Func<T,bool> merge=null)
            => source.SelectMany(arg => {
                merge ??= _ => true;
                var observable = Observable.Empty<T>();
                if (merge(arg)) {
                    observable = secondSelector(arg).IgnoreElements().To(arg);
                }
                return observable.StartWith(arg);
            });

        public static IObservable<T> ConcatIgnored<T,T2>(this IObservable<T> source,Func<T,IObservable<T2>> secondSelector,Func<T,bool> merge=null)
            => source.SelectMany(arg => {
                merge ??= _ => true;
                return merge(arg) ? secondSelector(arg).IgnoreElements().ConcatValue(arg) : arg.ReturnObservable();
            });
        public static IObservable<T> ConcatIgnored<T>(this IObservable<T> source,Action<T> action,Func<T,bool> merge=null)
            => source.SelectMany(arg => {
                merge ??= _ => true;
                if (merge(arg)) {
                    action(arg);
                    return Observable.Empty<T>().ConcatValue(arg);
                }
                return arg.ReturnObservable();
            });
        public static IObservable<T> ConcatIgnoredFirst<T,T2>(this IObservable<T> source,Func<T,IObservable<T2>> secondSelector,Func<T,bool> merge=null)
            => source.SelectMany((arg, i) => {
                var observable = Observable.Empty<T>();
                if (i == 0) {
                    merge ??= _ => true;
                    if (merge(arg)) {
                        observable = secondSelector(arg).IgnoreElements().To(arg);
                    }
                    return observable.Concat(arg.ReturnObservable());
                }

                return arg.ReturnObservable();

            });
        
    }
}