using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Create{
    public static partial class Create {
        public static IObservable<Tuple<TSource1, TSource2>> Permutations<TSource1, TSource2>(
            this IObservable<TSource1> source, IObservable<TSource2> other, IScheduler scheduler = null) 
            => Observable.Create<Tuple<TSource1, TSource2>>(observer => {
                var replay = other.Replay(scheduler ??= Scheduler.CurrentThread);
                var sequence = source.SelectMany(i => replay.Select(j => Tuple.Create(i, j)));
                return new CompositeDisposable(replay.Connect(), sequence.Subscribe(observer));
            });
    }
}