using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Xpand.Extensions.ObjectExtensions;

namespace Xpand.Extensions.Reactive.Combine {
    public static partial class Combine {
        public static IObservable<(TSource previous, TSource current)> CombineWithPrevious<TSource>(this IObservable<TSource> source,bool ensurePrevious=false) 
            => source.Scan((previous: default(TSource), current: default(TSource)), (t, current) => (t.current, current))
                .Select(t => (t.previous, t.current)).Where(t => !ensurePrevious||!t.previous.IsDefaultValue());
        public static IObservable<TSource> CombineWithPrevious<TSource>(this IObservable<TSource> source,Func<(TSource previous,TSource current),TSource> selector,bool emitPrevious=false) 
            => source.CombineWithPrevious(!emitPrevious).Select(selector);
        
        public static IObservable<(TSource previous, TSource current)> CombineWithPrevious<TSource>(this IObservable<TSource> source,Func<(TSource previous,TSource current),bool> selector,bool emitPrevious=false) 
            => source.CombineWithPrevious(!emitPrevious).Where(selector);
        
        public static IObservable<TSource> ToCurrent<TSource>(this IObservable<(TSource previous, TSource current)> source) 
            => source.Select(t => t.current);
        public static IObservable<TResult> CombineLatestWhenFirstEmits<TFirst, TSecond, TResult>(
            this IObservable<TFirst> first, IObservable<TSecond> second, Func<TFirst, TSecond, TResult> resultSelector)
            => Observable.Create<TResult>(observer => {
                var isFirstEmitted = false;
                TSecond lastSecond = default;
                var gate = new object();
                return new CompositeDisposable(first.Synchronize(gate).Subscribe(f => {
                        if (!isFirstEmitted) {
                            isFirstEmitted = true;
                        }
                        if (isFirstEmitted && !Equals(lastSecond, default(TSecond))) {
                            observer.OnNext(resultSelector(f, lastSecond));
                        }
                    },
                    observer.OnError,
                    observer.OnCompleted), second.Synchronize(gate).Subscribe(s => lastSecond = s));
            });

        public static IObservable<TResult> CombineVeryLatest<TLeft, TRight, TResult>(this IObservable<TLeft> leftSource,
            IObservable<TRight> rightSource, Func<TLeft, TRight, TResult> selector) 
            => Observable.Defer(() => {
                int l = -1, r = -1;
                return leftSource.Select(Tuple.Create<TLeft, int>).CombineLatest(
                        rightSource.Select(Tuple.Create<TRight, int>),
                        (x, y) => new { x, y })
                    .Where(t => t.x.Item2 != l && t.y.Item2 != r)
                    .Do(t => {
                        l = t.x.Item2;
                        r = t.y.Item2;
                    })
                    .Select(t => selector(t.x.Item1, t.y.Item1));
            });
    }
}