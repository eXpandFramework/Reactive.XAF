using System;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.ErrorHandling {
    public static partial class ErrorHandling {
		
        public static IObservable<T> CompleteOnError<T>(this IObservable<T> source,Action<Exception> onError=null,Func<Exception,bool> match=null)
            => source.Catch<T,Exception>(exception => {
                if (!(match?.Invoke(exception) ?? true)) return exception.Throw<T>();
                onError?.Invoke(exception);
                return Observable.Empty<T>();
            });
        
        public static IObservable<T> CompleteOnError<T,TException>(this IObservable<T> source,Action<Exception> onError=null) where TException:Exception
            => source.CompleteOnError(onError,exception => exception is TException);
        
        public static IObservable<T> CompleteOnError<T>(this IObservable<T> source,Type exceptionType,Action<Exception> onError=null) 
            => source.CompleteOnError(onError,exceptionType.IsInstanceOfType);
        public static IObservable<T> CompleteOnTimeout<T>(this IObservable<T> source) 
            => source.CompleteOnError(typeof(TimeoutException));
		
    }
}