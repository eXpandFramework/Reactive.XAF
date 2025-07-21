using System;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;

namespace Xpand.Extensions.Reactive.ErrorHandling {
    public static partial class ErrorHandling {
        public static IObservable<T> WrapError<T, TException>(this IObservable<T> source,
            Func<Exception, TException> exceptionFactory) where TException : Exception
            => source.Catch<T, Exception>(originalException => Observable.Throw<T>(exceptionFactory(originalException)));

        public static IObservable<T> AddErrorCallerContext<T>(this IObservable<T> source, [CallerMemberName] string caller = "") {
            return source.Catch<T, Exception>(exception => {
                exception.Data["OriginatingCaller"] = caller;
                return Observable.Throw<T>(exception);
            });
        }
        public static IObservable<T> WhenErrorAggregate<T>(this IObservable<T> source, [CallerMemberName] string caller = "")
            => source.Catch<T, Exception>(exception => new AggregateException(caller, exception).Throw<T>());
    }
}