using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Create {
    public static partial class Create {
        public static IObservable<TSource> RepeatUntilCompleted<TSource, TOther>(this IObservable<TSource> source, IObservable<TOther> other) 
            => Observable.Create<TSource>(observer => {
                var disposables = new CompositeDisposable();
                var completed = false;
                disposables.Add((other ??= (IObservable<TOther>)source).Subscribe(_ => { }, () => completed = true));
                disposables.Add(Observable.While(() => !completed, source).Subscribe(observer));
                return disposables;
            });
    }
}