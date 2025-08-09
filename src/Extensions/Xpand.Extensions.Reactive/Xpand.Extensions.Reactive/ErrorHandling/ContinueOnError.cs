using System;
using System.Runtime.CompilerServices;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;

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