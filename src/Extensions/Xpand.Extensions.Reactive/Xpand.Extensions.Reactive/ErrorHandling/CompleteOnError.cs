using System;
using System.Reactive.Linq;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Reactive.ErrorHandling {
    public static partial class ErrorHandling {
        public static IObservable<T> CompleteOnError<T>(this IObservable<T> source,Action<Exception> onError=null)
            => source.DoOnError(exception => onError?.Invoke(exception)).OnErrorResumeNext(Observable.Empty<T>());
    }
}