using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.Extensions.Reactive.Utility {
    
    public static partial class Utility {
        [Obsolete("Use Observable.Using instead")]
        public static IObservable<TResult> Use<T, TResult>(this T source, Func<T, IObservable<TResult>> selector) where T : IDisposable 
            => selector(source).Finally(source.Dispose);


        public static IObservable<T> SafeguardSubscription<T>(this IObservable<T> source,Action<Exception,string> onError, [CallerMemberName] string caller = "")
            => Observable.Create<T>(observer => {
                IDisposable subscription;
                try {
                    subscription = source.Subscribe(observer);
                }
                catch (Exception ex) {
                    onError(ex, caller);
                    observer.OnCompleted();
                    return Disposable.Empty;
                }

                return Disposable.Create(subscription, disposable => {
                    try {
                        disposable.Dispose();
                    }
                    catch (Exception ex) {
                        onError(ex, caller);
                    }
                });
            });

    }
}