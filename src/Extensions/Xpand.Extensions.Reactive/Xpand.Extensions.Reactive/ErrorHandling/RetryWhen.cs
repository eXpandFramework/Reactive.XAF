using System;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.ErrorHandling {
    public static partial class ErrorHandling {
        public static IObservable<T> RetryWhen<T,TException>(this IObservable<T> source) where TException : Exception 
            => source.RetryWhen(obs => obs.OfType<TException>());
        public static IObservable<T> RetryWhenObjectDisposed<T>(this IObservable<T> source) 
            => source.RetryWhen<T,ObjectDisposedException>();
    }
}