using System;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.ErrorHandling {
	public enum FaultAction {
		Rethrow,
		Complete
	}
	public static partial class ErrorHandling {
		private static IObservable<T> RegisterHandler<T>(this IObservable<T> source, Func<Exception, FaultAction?> handler) 
			=> Observable.Defer(() => source.Finally(handler.AddHandler().Dispose));
		
		public static IObservable<T> CompleteOnError<T>(this IObservable<T> source, Func<Exception, bool> predicate)
			=> source.CompleteOnError(_ => { },predicate);
		public static IObservable<T> PublishOnError<T>(this IObservable<T> source, Func<Exception, bool> predicate)
			=> source.PublishOnError(_ => {},predicate);
		public static IObservable<T> PublishOnError<T>(this IObservable<T> source, Action<Exception> onError = null, Func<Exception, bool> match = null) 
			=> source.CompleteOnError(onError, match, mute: false);

		public static IObservable<T> CompleteOnError<T>(this IObservable<T> source, Action<Exception> onError = null, Func<Exception, bool> match = null, bool mute = true) {
			var predicate = match ?? (_ => true);

			FaultAction? Handler(Exception ex) {
				if (!predicate(ex)) return null;
				if (mute) {
					ex.MuteForBus();
				}
				onError?.Invoke(ex);
				return FaultAction.Complete;
			}
			return source.RegisterHandler(Handler);
		}
		public static IObservable<T> CompleteOnError<T,TException>(this IObservable<T> source,Action<Exception> onError=null) where TException:Exception
			=> source.CompleteOnError(onError,exception => exception is TException);
      
		public static IObservable<T> CompleteOnError<T>(this IObservable<T> source,Type exceptionType,Action<Exception> onError=null) 
			=> source.CompleteOnError(onError,exceptionType.IsInstanceOfType);

		public static IObservable<T> CompleteOnTimeout<T>(this IObservable<T> source) 
			=> source.CompleteOnError(typeof(TimeoutException));		
	}
}