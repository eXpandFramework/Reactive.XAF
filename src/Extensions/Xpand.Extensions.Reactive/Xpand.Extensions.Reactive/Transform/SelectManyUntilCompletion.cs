using System;
using System.Collections.Concurrent;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;

namespace Xpand.Extensions.Reactive.Transform {
    public static partial class Transform {
        
        
        
        
        
        
        
        public static IObservable<TResult> SelectManyUntilCompletion<TSource, TResult>(this IObservable<TSource> source,
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