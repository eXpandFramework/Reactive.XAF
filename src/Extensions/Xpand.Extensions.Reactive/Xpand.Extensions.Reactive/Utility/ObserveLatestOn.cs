using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;

namespace Xpand.Extensions.Reactive.Utility{
    public static partial class Utility {
        public static IObservable<TSource> ObserveLatestOn<TSource>(this IObservable<TSource> source, SynchronizationContext context)
            => source.ObserveLatestOn(new SynchronizationContextScheduler(context));
        public static IObservable<TSource> ObserveLatestOnContext<TSource>(this IObservable<TSource> source)
            => source.ObserveLatestOn(SynchronizationContext.Current);
        
        public static IObservable<TSource> ObserveLatest<TSource>(this IObservable<TSource> source)
            => source.ObserveLatestOn(System.Reactive.Concurrency.Scheduler.CurrentThread);
        public static IObservable<TSource> ObserveLatestOn<TSource>(this IObservable<TSource> source, IScheduler scheduler) 
            => Observable.Create<TSource>(observer => {
                Notification<TSource> pendingNotification = null;
                var cancelable = new MultipleAssignmentDisposable();
                var sourceSubscription = source.Materialize()
                    .Subscribe(notification => {
                        var previousNotification = Interlocked.Exchange(ref pendingNotification, notification);
                        if (previousNotification != null) return;
                        cancelable.Disposable = scheduler.Schedule(() => {
                            var notificationToSend = Interlocked.Exchange(ref pendingNotification, null);
                            notificationToSend!.Accept(observer);
                        });
                    });
                return new CompositeDisposable(sourceSubscription, cancelable);
            });
    }
}