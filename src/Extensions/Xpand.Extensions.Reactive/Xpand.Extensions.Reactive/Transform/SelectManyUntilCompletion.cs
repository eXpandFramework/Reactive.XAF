using System;
using System.Collections.Concurrent;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;

namespace Xpand.Extensions.Reactive.Transform {
    public static partial class Transform {
        /// <summary>
        /// Projects each element of the source observable sequence to a subsequence,
        /// and merges the resulting subsequences into one observable sequence.
        /// The merged sequence completes when all the projected subsequences complete
        /// on their own. Unlike the SelectMany operator, the subsequences are not
        /// unsubscribed when an error occurs.
        /// </summary>
        public static IObservable<TResult> SelectManyUntilCompletion<TSource, TResult>(
            this IObservable<TSource> source,
            Func<TSource, CancellationToken, IObservable<TResult>> selector) 
            => Observable.Defer(() => {
                var cts = new CancellationTokenSource();
                var errors = new ConcurrentQueue<Exception>();
                var stopSignal = new Subject<Unit>();
                var stopSignalSynchronized = Observer.Synchronize(stopSignal);

                IObservable<T> HandleErrorReturnEmpty<T>(Exception ex) {
                    cts.Cancel();
                    bool ignoreError = ex is OperationCanceledException
                                       && cts.IsCancellationRequested;
                    if (!ignoreError) errors.Enqueue(ex);
                    stopSignalSynchronized.OnNext(default);
                    return Observable.Empty<T>();
                }

                return source
                    .TakeUntil(stopSignal)
                    .Catch((Exception ex) => HandleErrorReturnEmpty<TSource>(ex))
                    .SelectMany(item => {
                        if (!errors.IsEmpty) return Observable.Empty<TResult>();
                        IObservable<TResult> projected;
                        try {
                            projected = selector(item, cts.Token);
                        }
                        catch (Exception ex) {
                            return HandleErrorReturnEmpty<TResult>(ex);
                        }

                        return projected
                            .Catch((Exception ex) => HandleErrorReturnEmpty<TResult>(ex));
                    })
                    .Concat(Observable.Defer(() => {
                        cts.Dispose();
                        if (!errors.IsEmpty) throw new AggregateException(errors);
                        return Observable.Empty<TResult>();
                    }));
            });
    }
}