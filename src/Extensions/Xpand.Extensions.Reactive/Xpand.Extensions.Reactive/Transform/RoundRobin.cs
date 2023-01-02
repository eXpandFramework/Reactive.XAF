using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks.Dataflow;

namespace Xpand.Extensions.Reactive.Transform{
	
	public static partial class Transform{
		[SuppressMessage("ReSharper", "HeapView.CanAvoidClosure")]
		public static IObservable<T> RoundRobin<T>(this IObservable<T> source){
			
			return Observable.Create<T>(observer => {
				var cache = new BlockingCollection<T>();

				source.Do(i => cache.TryAdd(i)).Subscribe();

				return () => cache.ToObservable(Scheduler.Default).Do(observer);
			});
		}
		// public static IObservable<T> RoundRobin<T>(this IObservable<T> source){
		// 	var subscribers = new BehaviorSubject<ImmutableList<IObserver<T>>>(ImmutableList<IObserver<T>>.Empty);
		// 	ImmutableList<IObserver<T>> latest = ImmutableList<IObserver<T>>.Empty;
		// 	subscribers.Subscribe(l => latest = l);
		//
		// 	var shared = source
		// 		.Select((v, i) => (v, i))
		// 		.WithLatestFrom(subscribers, (t, s) => (t.v, t.i, s))
		// 		.Publish()
		// 		.RefCount();
		// 	return Observable.Create<T>(observer => {
		// 		subscribers.OnNext(latest.Add(observer));
		// 		var dispose = Disposable.Create(() => subscribers.OnNext(latest.Remove(observer)));
		//
		// 		var sub = shared
		// 			.Where(t => t.i % t.s.Count == t.s.FindIndex(o => o == observer))
		// 			.Select(t => t.v)
		// 			.Subscribe(observer);
		//
		// 		return new CompositeDisposable(dispose, sub);
		// 	});
		// }
		public static IObservable<T> Distribute<T>(this IObservable<T> source) {
			var buffer = new BufferBlock<T>();
			source.Subscribe(buffer.AsObserver());             
			return Observable.Create<T>(observer => buffer.LinkTo(
				new ActionBlock<T>(observer.OnNext, new ExecutionDataflowBlockOptions { BoundedCapacity = 1 })
			));
		}
	}
}