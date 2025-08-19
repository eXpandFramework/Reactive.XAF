using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Linq;
using System.Threading.Tasks.Dataflow;

namespace Xpand.Extensions.Reactive.Transform{
	
	public static partial class Transform{
		[SuppressMessage("ReSharper", "HeapView.CanAvoidClosure")]
		public static IObservable<T> RoundRobin<T>(this IObservable<T> source){
			
			return Observable.Create<T>(observer => {
				var cache = new BlockingCollection<T>();

				source.Do(i => cache.TryAdd(i)).Subscribe();

				return () => cache.ToObservable(global::System.Reactive.Concurrency.Scheduler.Default).Do(observer);
			});
		}
		
		
		
		
		
		
		
		
		
		
		
		
		
		
		
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