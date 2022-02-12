using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Utility {
    public static partial class Utility {
        /// <summary>
        /// Invokes a specified action after the source observable sequence terminates
        /// successfully or exceptionally. The action is invoked before the propagation
        /// of the source's completion, and any exception thrown by the action is
        /// propagated to the observer. The action is also invoked if the observer
        /// is unsubscribed before the termination of the source sequence.
        /// </summary>
        public static IObservable<T> FinallySafe<T>(this IObservable<T> source, Action finallyAction) 
            => Observable.Create<T>(observer => {
                var finallyOnce = Disposable.Create(finallyAction);
                var subscription = source.Subscribe(observer.OnNext, error => {
                    try {
                        finallyOnce.Dispose();
                    }
                    catch (Exception ex) {
                        observer.OnError(ex);
                        return;
                    }

                    observer.OnError(error);
                }, () => {
                    try {
                        finallyOnce.Dispose();
                    }
                    catch (Exception ex) {
                        observer.OnError(ex);
                        return;
                    }

                    observer.OnCompleted();
                });
                return new CompositeDisposable(subscription, finallyOnce);
            });
    }
}