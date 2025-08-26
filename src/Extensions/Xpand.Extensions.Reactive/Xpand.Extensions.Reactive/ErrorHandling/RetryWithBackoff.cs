using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using Xpand.Extensions.ObjectExtensions;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Transform.System;

namespace Xpand.Extensions.Reactive.ErrorHandling {
    public static partial class ErrorHandling {
        
        public static readonly Func<int, TimeSpan> SecondsBackoffStrategy = n => TimeSpan.FromSeconds(Math.Min(Math.Pow(2, n), 180));
        public static readonly Func<int, TimeSpan> MilliSecondsBackoffStrategy = n => TimeSpan.FromMilliseconds(Math.Min(Math.Pow(2, n), 180)*200);
        
        public static IObservable<T> RetryWithBackoff<T>(this IObservable<T> source,Func<Exception, IObservable<Unit>> retryOnError , int? retryCount = null, Func<int, TimeSpan> strategy = null,
            IScheduler scheduler = null,[CallerMemberName]string caller="") {
            strategy ??= SecondsBackoffStrategy;
            scheduler ??= DefaultScheduler.Instance;
            retryOnError ??= _ => Unit.Default.Observe();
            return Observable.Defer(() => {
                var attempt = 0;
                return Resubscribe();
                IObservable<T> Resubscribe() => source.Catch<T, Exception>(ex => {
                    Console.WriteLine(caller.PrefixCaller());
                    
                    if (ex is DoNotRetryWithBackoffException exception)
                        return exception.InnerException.Throw<T>();
                    if (retryCount.HasValue && ++attempt >= retryCount.Value)
                        return ex.Throw<T>();
                    return retryOnError(ex).Take(1)
                        .SelectMany(_ => strategy(attempt).Timer(scheduler)
                            .SelectMany(_ => Resubscribe()))
                        .SwitchIfEmpty(ex.Throw<T>());
                    
                });
            });
        }

        public static IObservable<T> DoNotRetryWithBackoff<T>(this IObservable<T> source)
            => source.Catch<T, Exception>(e => new DoNotRetryWithBackoffException(e).Throw<T>());
        
        public static IObservable<T> RetryWithBackoff<T>(this IObservable<T> source, int? retryCount = null, Func<int, TimeSpan> strategy = null,
            Func<Exception, bool> retryOnError = null, IScheduler scheduler = null,[CallerMemberName]string caller="")
            => source.RetryWithBackoff(exception => (retryOnError?.Invoke(exception)??true)?Unit.Default.Observe(): Observable.Empty<Unit>(),retryCount,strategy,scheduler,caller);
        
        
    }
    public class DoNotRetryWithBackoffException(Exception exception) : Exception(exception.Message, exception);
}