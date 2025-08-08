using System;
using System.Runtime.CompilerServices;

namespace Xpand.Extensions.Reactive.ErrorHandling {
    public static partial class ErrorHandling {
        public static IObservable<T> ContinueOnError<T>(this IObservable<T> source, object[] context=null,[CallerMemberName]string caller="") 
            => source.ChainFaultContext(context,caller).PublishOnError();
        public static IObservable<T> ContinueOnError<T>(this IObservable<T> source,
            Func<IObservable<T>, IObservable<T>> retryStrategy, object[] context = null,
            [CallerMemberName] string caller = "")
            => source.ChainFaultContext(retryStrategy, context, caller).PublishOnError();
    }
}