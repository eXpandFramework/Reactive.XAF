using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using akarnokd.reactive_extensions;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.Extensions.Reactive.Utility {
    public static partial class Utility {
        public static IObservable<T> ObserveOnWindows<T>(this IObservable<T> source, SynchronizationContext synchronizationContext) =>
            AppDomain.CurrentDomain.IsHosted() ? source : source.ObserveOn(synchronizationContext);
        public static IObservable<T> ObserveOnDefault<T>(this IObservable<T> source)
            => source.ObserveOn(DefaultScheduler.Instance);
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
        {
            return Observable.Create<T>(observer =>
            {
                var cancellation = new CancellationDisposable();
                var token = cancellation.Token;
                Task task = null;

                var subscription = source.Subscribe(
                    value =>
                    {
                        if (task == null || task.IsCompleted)
                        {
                            task = Task.Factory.StartNew(() => 
                            {
                                try
                                {
                                    // action(value, token);
                                    observer.OnNext(value); // Notify observer
                                }
                                catch (Exception ex)
                                {
                                    observer.OnError(ex); // Propagate error
                                }
                            }, token);
                        }
                    },
                    observer.OnError, // Propagate any errors
                    observer.OnCompleted // Notify completion
                );

                return new CompositeDisposable(subscription, cancellation);
            }).ObserveOn(scheduler);
        }    
    }
}