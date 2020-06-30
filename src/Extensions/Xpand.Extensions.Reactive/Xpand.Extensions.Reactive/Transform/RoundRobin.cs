using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Xpand.Extensions.LinqExtensions;

namespace Xpand.Extensions.Reactive.Transform{
	public static partial class Transform{
		public static IObservable<T> RoundRobin<T>(this IObservable<T> source){
			var subscribers = new List<IObserver<T>>();
			var shared = source
				.Zip(subscribers.Repeat(), (value, observer) => (value, observer))
				.Publish()
				.RefCount();

			return Observable.Create<T>(observer => {
				subscribers.Add(observer);
				var subscription = shared.Where(pair => pair.observer == observer)
						.Select(pair => pair.value)
						.Subscribe(observer);

				var dispose = Disposable.Create(() => subscribers.Remove(observer));
				return new CompositeDisposable(subscription, dispose);
			});
		}
	}
}