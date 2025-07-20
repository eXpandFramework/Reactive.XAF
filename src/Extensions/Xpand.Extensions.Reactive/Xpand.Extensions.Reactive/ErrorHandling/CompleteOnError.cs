using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Xpand.Extensions.ExceptionExtensions;

namespace Xpand.Extensions.Reactive.ErrorHandling {
	public static partial class ErrorHandling {
		
		public static IObservable<T> CompleteOnError<T>(this IObservable<T> source, Action<Exception> onError = null,
			Func<Exception, bool> match = null,[CallerMemberName]string caller="")
			=> source
				.WithOrigin()
				.Catch<T, Exception>(e => {
					onError?.Invoke(e);
					var invoke = match?.Invoke(e);
					return !(invoke ?? true) ? Observable.Throw<T>(e) : Observable.Empty<T>();
				});

		public static IObservable<T> CompleteOnError<T,TException>(this IObservable<T> source,Action<Exception> onError=null) where TException:Exception
			=> source.CompleteOnError(onError,exception => exception is TException);
        
		public static IObservable<T> CompleteOnError<T>(this IObservable<T> source,Type exceptionType,Action<Exception> onError=null) 
			=> source.CompleteOnError(onError,exceptionType.IsInstanceOfType);
		public static IObservable<T> CompleteOnTimeout<T>(this IObservable<T> source) 
			=> source.CompleteOnError(typeof(TimeoutException));
		
	}
}