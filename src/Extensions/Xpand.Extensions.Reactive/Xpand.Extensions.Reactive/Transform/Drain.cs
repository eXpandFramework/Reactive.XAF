using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Xpand.Extensions.Reactive.Transform{
    public static partial class Transform{
        public static IObservable<TOut> Drain<TSource, TOut>(this IObservable<TSource> source,
            Func<TSource, IObservable<TOut>> selector) =>
            Observable.Defer(() => {
                var queue = new BehaviorSubject<Unit>(new Unit());

                return source
                    .Zip(queue, (v, q) => v)
                    .SelectMany(v => selector(v)
                        .Do(_ => { }, () => queue.OnNext(new Unit()))
                    );
            });
    }
}