using System;
using System.Reactive;
using System.Reactive.Linq;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Transform.System;

namespace Xpand.Extensions.Reactive.ErrorHandling {
    public static partial class ErrorHandling {
        public static IObservable<T> RetryWhen<T,TException>(this IObservable<T> source) where TException : Exception 
            => source.RetryWhen(obs => obs.OfType<TException>());
        
        public static IObservable<T> RetryWhen<T,TException>(this IObservable<T> source,Func<TException,IObservable<Unit>> signal) where TException : Exception 
            => source.RetryWhen(obs => obs.OfType<TException>().SelectMany(signal));
        public static IObservable<T> RetryWhen<T,TException>(this IObservable<T> source,TimeSpan signal) where TException : Exception 
            => source.RetryWhen<T,TException>(_ => signal.Timer().ToUnit());
        public static IObservable<T> RetryWhen<T>(this IObservable<T> source,TimeSpan signal) 
            => source.RetryWhen<T,Exception>(signal);
        
        public static IObservable<T> RetryWhenObjectDisposed<T>(this IObservable<T> source) 
            => source.RetryWhen<T,ObjectDisposedException>();
        
        

    }
}