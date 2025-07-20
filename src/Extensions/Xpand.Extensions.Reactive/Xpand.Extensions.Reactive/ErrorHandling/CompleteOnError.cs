using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Xpand.Extensions.ExceptionExtensions;

namespace Xpand.Extensions.Reactive.ErrorHandling {
	public static partial class ErrorHandling {
// Add this helper method to your extensions class
		private static IObservable<T> RegisterHandler<T>(this IObservable<T> source, Func<Exception, bool> predicate) {
			return Observable.Defer(() => {
				if (FaultHub.HandlersContext.Value == null) FaultHub.HandlersContext.Value = new List<Func<Exception, bool>>();
				FaultHub.HandlersContext.Value.Add(predicate);
				return source.Finally(() => {
					FaultHub.HandlersContext.Value?.Remove(predicate);
				});
			});
		}

// Replace ALL existing CompleteOnError overloads with this block
		public static IObservable<T> CompleteOnError<T>(this IObservable<T> source, Action<Exception> onError = null,
			Func<Exception, bool> match = null, bool mute = true, [CallerMemberName] string caller = "") {
			var predicate = match ?? (_ => true);
			return source
				.RegisterHandler(predicate)
				.Catch<T, Exception>(e => {
					if (predicate(e)) {
						if (mute) {
							e.MuteForBus();
						}
						onError?.Invoke(e);
						return Observable.Empty<T>();
					}
					return Observable.Throw<T>(e);
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