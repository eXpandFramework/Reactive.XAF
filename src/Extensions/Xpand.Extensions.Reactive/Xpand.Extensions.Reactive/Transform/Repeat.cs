using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using Xpand.Extensions.Fasterflect;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Transform.System;
using Xpand.Extensions.TypeExtensions;

namespace Xpand.Extensions.Reactive.Transform {
    public static partial class Transform {
        public static IObservable<T> RepeatDefaultValueDuringSilence<T>(this IObservable<T> source,
            TimeSpan maxQuietPeriod, IScheduler scheduler = null)
            => source.RepeatDuringSilence(maxQuietPeriod, _ => typeof(T).DefaultValue().Observe().Cast<T>(),scheduler);

        public static IObservable<T> RepeatNewInstanceDuringSilence<T>(this IObservable<T> source, TimeSpan maxQuietPeriod, IScheduler scheduler = null) 
            =>  source.RepeatDuringSilence(maxQuietPeriod, _ => typeof(T).CreateInstance<T>().Observe(),scheduler);
        public static IObservable<T> RepeatDuringSilence<T>(this IObservable<T> source, TimeSpan maxQuietPeriod,
            Func<T, IObservable<T>> observableSelector, IScheduler scheduler = null) 
            =>  source.Select(x => Observable.Interval(maxQuietPeriod,scheduler??Scheduler.Default)
                .Select(_ => observableSelector(x)).Concat().StartWith(x)).Switch();
        public static IObservable<T[]> RepeatEmptyDuringSilence<T>(this IObservable<T[]> source, TimeSpan maxQuietPeriod, IScheduler scheduler = null)
            => source.RepeatDuringSilence(maxQuietPeriod, _ => Array.Empty<T>().Observe());
        public static IObservable<T> RepeatLastValue<T>(this IObservable<T> source,
            Func<T,IObservable<object>> when, IScheduler scheduler = null) {
            scheduler ??= Scheduler.Default;
            return source.Select(x => when(x).Select(_ => x).StartWith(scheduler, x))
                .Switch();
        }

        public static IObservable<T> RepeatLastValueDuringSilence<T>(this IObservable<T> source,
            TimeSpan maxQuietPeriod, IScheduler scheduler = null) {
            scheduler ??= Scheduler.Default;
            return source.Select(x => Observable.Interval(maxQuietPeriod, scheduler).Select(_ => x).StartWith(scheduler, x))
                .Switch();
        }

        public static IObservable<TSource> RepeatUntilCompleted<TSource, TOther>(this IObservable<TSource> source, IObservable<TOther> other) 
            => Observable.Create<TSource>(observer => {
                var disposables = new CompositeDisposable();
                var completed = false;
                disposables.Add((other ??= (IObservable<TOther>)source).Subscribe(_ => { }, () => completed = true));
                disposables.Add(Observable.While(() => !completed, source).Subscribe(observer));
                return disposables;
            });

        public static IObservable<T> RepeatWhen<T>(this IObservable<T> source, TimeSpan timeSpan)
            => source.RepeatWhen(obs => obs.SelectMany(_ => timeSpan.Timer()));
        
        public static IObservable<T> RepeatUntilEmpty<T>(this IObservable<T> source,Func<T[],bool> takeUntil=null) 
            => Observable.Defer(() => source.BufferUntilCompleted())
                .Repeat().TakeWhile(list => list.Length>0&& (takeUntil?.Invoke(list) ?? true))
                .SelectMany()
                .Select(o => o);
        
        public static IObservable<T> RepeatWhenEmpty<T>(this IObservable<T> source, int? maxRetries=null,[CallerMemberName]string caller="") 
            => Observable.Defer(() => source.SwitchIfEmpty(Observable.Defer(() => new Exception(caller).Throw<T>())))
                .Retry(maxRetries??Int32.MaxValue).Catch<T,Exception>(exception => exception.Throw<T>());
    }
}