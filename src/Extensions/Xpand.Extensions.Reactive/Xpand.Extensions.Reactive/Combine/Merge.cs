using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.Extensions.Reactive.Combine{
    public static partial class Combine{
        public static IObservable<TC> MergeOrCombineLatest<TA, TB, TC>(this IObservable<TA> a, IObservable<TB> b, Func<TA, TC> aStartsFirst, Func<TB, TC> bStartFirst, Func<TA, TB, TC> bothStart)
            => a.Publish(aa => b.Publish(bb => aa.CombineLatest(bb, bothStart)
                    .Publish(xs => aa.Select(aStartsFirst).Merge(bb.Select(bStartFirst)).TakeUntil(xs).SkipLast(1).Merge(xs))));
        public static IObservable<T> MergeOrdered<T>(this IObservable<IObservable<T>> source, int maximumConcurrency = Int32.MaxValue) 
            => Observable.Defer(() => {
                var semaphore = new SemaphoreSlim(maximumConcurrency);
                return source.Select(inner => {
                        var published = inner.Replay();
                        _ = semaphore.WaitAsync().ContinueWith(_ => published.Connect(), TaskScheduler.Default);
                        return published.Finally(() => semaphore.Release());
                    })
                    .Concat();
            });

        public static IObservable<TSource> MergeWith<TSource>(this IObservable<TSource> source,
            Func<TSource, IObservable<TSource>> selector, IScheduler scheduler = null)
            => source.Publish(obs => obs.Merge(obs.SelectMany(selector),scheduler??Scheduler.Default));
        
        public static IObservable<TValue> MergeWith<TSource, TValue>(this IObservable<TSource> source, TValue value, IScheduler scheduler = null) 
            => source.Merge(default(TSource).Observe(scheduler ?? CurrentThreadScheduler.Instance)).Select(_ => value);

        public static IObservable<Unit> MergeToUnit<TSource, TValue>(this IObservable<TSource> source, IObservable<TValue> value, IScheduler scheduler = null) 
            => source.ToUnit().Merge(value.ToUnit());
        
        public static IObservable<TValue> MergeTo<TSource, TValue>(this IObservable<TSource> source, IObservable<TValue> value, IScheduler scheduler = null) where TValue:class 
            => source.Select(source1 => source1 as TValue).WhenNotDefault().Merge(value.To<TValue>());
        
        public static IObservable<object> MergeToObject<TSource, TValue>(this IObservable<TSource> source, IObservable<TValue> value, IScheduler scheduler = null) where TValue:class 
            => source.Select(source1 => source1 as object).WhenNotDefault().Merge(value.To<TValue>());

        public static IObservable<T> MergeIgnored<T,T2>(this IObservable<T> source,Func<T,IObservable<T2>> secondSelector,Func<T,bool> merge=null)
            => source.Publish(obs => obs.SelectMany(arg => {
                merge ??= _ => true;
                var observable = Observable.Empty<T>();
                if (merge(arg)) {
                    observable = secondSelector(arg).IgnoreElements().To(arg);
                }
                return arg.Observe().Merge(observable);
            }));
        
        public static IObservable<T> MergeIgnored<T,T2>(this IObservable<T> source,Func<T,bool> merge,Func<T,IObservable<T2>> secondSelector)
            => source.MergeIgnored(secondSelector);
        
        public static IObservable<T> MergeIgnored<T>(this IObservable<T> source,Func<T,bool> merge,Action<T> @do)
            => source.MergeIgnored(_ => Observable.Empty<T>(), arg => {
                @do(arg);
                return merge(arg);
            });

        public static IObservable<T> MergeFollow<T>(this IObservable<T> source, IObservable<T> target,int take=1)
            => source.Publish(ticker => target.Merge(ticker).Take(take).Concat(ticker));
    }
}