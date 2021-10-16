using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Create{
    public static partial class Create {
        public static IObservable<Tuple<TSource1, TSource2>> CreatePermutations<TSource1, TSource2>(
            this IObservable<TSource1> source, IObservable<TSource2> other, IScheduler scheduler = null) {
            scheduler ??= Scheduler.CurrentThread;
            return Observable.Create<Tuple<TSource1, TSource2>>(observer => {
                var replay = other.Replay(scheduler);
                var sequence = source.SelectMany(i => replay.Select(j => Tuple.Create(i, j)));
                return new CompositeDisposable(replay.Connect(), sequence.Subscribe(observer));
            });
        }
    }
}