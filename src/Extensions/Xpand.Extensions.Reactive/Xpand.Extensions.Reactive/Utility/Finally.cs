using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace Xpand.Extensions.Reactive.Utility {
    public static partial class Utility {
        public static IObservable<T> AsyncFinally<T>(this IObservable<T> source, Func<IObservable<object>> action)
            => source.AsyncFinally(async () => await action().ToTask());

        public static IObservable<T> AsyncFinally<T>(this IObservable<T> source, Func<System.Threading.Tasks.Task> action) 
            => source
                .Materialize()
                .SelectMany(async n => {
                    switch (n.Kind){
                        case NotificationKind.OnCompleted:
                        case NotificationKind.OnError:
                            await action();
                            return n;
                        case NotificationKind.OnNext:
                            return n;
                        default:
                            throw new NotImplementedException();
                    }
                })
                .Dematerialize();
        
        public static IObservable<T> FinallySafe<T>(this IObservable<T> source, Action finallyAction,[CallerMemberName]string caller="" ) 
            => Observable.Create<T>(observer => {
                var finallyOnce = Disposable.Create(finallyAction);
                var subscription = source.Subscribe(observer.OnNext, error => {
                    try {
                        finallyOnce.Dispose();
                    }
                    catch (Exception ex) {
                        ex.Source = caller;
                        observer.OnError(ex);
                        return;
                    }
                    error.Source = caller;
                    observer.OnError(error);
                }, () => {
                    try {
                        finallyOnce.Dispose();
                    }
                    catch (Exception ex) {
                        ex.Source = caller;
                        observer.OnError(ex);
                        return;
                    }

                    observer.OnCompleted();
                });
                return new CompositeDisposable(subscription, finallyOnce);
            });


        public static IObservable<T> DoFinallySafe<T>(this IObservable<T> source, Action finallyAction) 
            => source.FinallySafe(() => {
                try {
                    finallyAction();
                }
                catch (Exception) {
                    // ignored
                }
            });
    }
}