using System;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.ErrorHandling {
	public static partial class ErrorHandling {
		private static IObservable<T> RegisterHandler<T>(this IObservable<T> source, Func<Exception, bool> predicate) 
			=> Observable.Defer(() => {
				FaultHub.HandlersContext.Value ??= [];
				FaultHub.HandlersContext.Value.Add(predicate);
				return source.Finally(() => FaultHub.HandlersContext.Value?.Remove(predicate));
			});


		public static IObservable<T> CompleteOnError<T>(this IObservable<T> source, Action<Exception> onError = null, Func<Exception, bool> match = null, bool mute = true) {
			var predicate = match ?? (_ => true);
			return source.RegisterHandler(predicate)
				.Catch<T, Exception>(e => {
					if (!predicate(e)) return Observable.Throw<T>(e);
					if (mute) {
						e.MuteForBus();
					}
					onError?.Invoke(e);
					return Observable.Empty<T>();
				});
		}

		public static IObservable<T> CompleteOnError<T,TException>(this IObservable<T> source,Action<Exception> onError=null) where TException:Exception
			=> source.CompleteOnError(onError,exception => exception is TException);
      
		public static IObservable<T> CompleteOnError<T>(this IObservable<T> source,Type exceptionType,Action<Exception> onError=null) 
			=> source.CompleteOnError(onError,exceptionType.IsInstanceOfType);

		public static IObservable<T> CompleteOnTimeout<T>(this IObservable<T> source) 
			=> source.CompleteOnError(typeof(TimeoutException));		
	}
}