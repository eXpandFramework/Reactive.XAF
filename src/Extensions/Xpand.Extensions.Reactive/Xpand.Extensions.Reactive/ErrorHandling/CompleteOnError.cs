using System;
using System.Reactive.Linq;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Reactive.ErrorHandling {
	public static partial class ErrorHandling {
		public static IObservable<T> CompleteOnError<T>(this IObservable<T> source,Action<Exception> onError=null)
			=> source.DoOnError(exception => onError?.Invoke(exception)).OnErrorResumeNext(Observable.Empty<T>());
        
		public static IObservable<T> CompleteOnError<T,TException>(this IObservable<T> source,Action<Exception> onError=null) where TException:Exception
			=> source.Catch<T,TException>(exception => {
				onError?.Invoke(exception);
				return Observable.Empty<T>();
			});
        
		public static IObservable<T> CompleteOnError<T>(this IObservable<T> source,Type exceptionType,Action<Exception> onError=null) 
			=> source.Catch<T,Exception>(exception => {
				if (exceptionType.IsInstanceOfType(exception)) {
					onError?.Invoke(exception);
					return Observable.Empty<T>();    
				}
				return Observable.Throw<T>(exception!);
			});
	}
}