using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.Extensions.Reactive.Utility {
    public static partial class Utility {
        public static IObservable<T> ObserveOnWindows<T>(this IObservable<T> source, SynchronizationContext synchronizationContext) =>
            AppDomain.CurrentDomain.IsHosted() ? source : source.ObserveOn(synchronizationContext);
        public static IObservable<T> ObserveOnDefault<T>(this IObservable<T> source) {
            TaskPoolScheduler.Default.DisableOptimizations(typeof(ISchedulerLongRunning));
            return source.ObserveOn(DefaultScheduler.Instance);
        }

        public static IObservable<T> ObserveOnContext<T>(this IObservable<T> source, SynchronizationContext synchronizationContext) 
            => source.ObserveOn(synchronizationContext);

        public static IObservable<T> ObserveOnContext<T>(this IObservable<T> source, bool throwIfNull) 
            => source.If(_ => throwIfNull && SynchronizationContext.Current == null,
                () => new NullReferenceException(nameof(SynchronizationContext)).Throw<T>(), () => source);

        public static IObservable<T> ObserveOnContext<T>(this IObservable<T> source) {
            var synchronizationContext = SynchronizationContext.Current;
            return synchronizationContext != null ? source.ObserveOn(synchronizationContext) : source;
        }
        
        public static IObservable<T> ObserveOn<T>(this IObservable<T> source, SynchronizationContext synchronizationContext,Func<bool> state)
            => !state() ? source : source.ObserveOn(synchronizationContext);
        
        public static IObservable<TSource> ObserveLatestOn<TSource>(this IObservable<TSource> source, SynchronizationContext context)
            => source.ObserveLatestOn(new SynchronizationContextScheduler(context));
        public static IObservable<TSource> ObserveLatestOnContext<TSource>(this IObservable<TSource> source)
            => source.ObserveLatestOn(SynchronizationContext.Current);
        
        public static IObservable<TSource> ObserveLatest<TSource>(this IObservable<TSource> source)
            => source.ObserveLatestOn(System.Reactive.Concurrency.Scheduler.CurrentThread);
        
        public static IObservable<T> ObserveLatestOn<T>(this IObservable<T> source, IScheduler scheduler) 
            => Observable.Create<T>(observer => {
                var cancellation = new CancellationDisposable();
                Task task = null;
                return new CompositeDisposable(source.Subscribe(value => {
                    if (task is { IsCompleted: false }) return;
                    task = Task.Factory.StartNew(() => {
                        try {
                            observer.OnNext(value);
                        }
                        catch (Exception ex) {
                            observer.OnError(ex); 
                        }
                    }, cancellation.Token);
                },
                observer.OnError, observer.OnCompleted 
            ), cancellation);
        }).ObserveOn(scheduler);
    }
}