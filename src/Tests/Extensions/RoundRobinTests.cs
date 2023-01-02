using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.Transform;
using Xpand.TestsLib;

namespace Xpand.Extensions.Tests{
	public class RoundRobinTests:BaseTest{
		[Test]
		public async Task RoundRobin() {
			var a = new List<int>();
			var b = new List<int>();
			
			var source = Observable.Range(1, 20);
			
			// var cache = new BlockingCollection<int>();
			
			// source.Do(i => cache.TryAdd(i)).Publish().RefCount().Subscribe();
			
			
			// var roundRobin = cache.GetConsumingEnumerable().ToObservable(Scheduler.Default);
			var roundRobin = source.RoundRobin().Publish().RefCount();
			
			roundRobin.Subscribe(i => a.Add(i));
			roundRobin.Subscribe(i => b.Add(i));

			// source.Connect();
			await Task.Delay(TimeSpan.FromSeconds(2));
			a.Count.ShouldBeGreaterThan(0);
			b.Count.ShouldBeGreaterThan(0);
			a.Concat(b).Count().ShouldBe(20);
			a.Intersect(b).Count().ShouldBe(0);

		}
		[Test]
		public async Task RoundRobin2() {
			var a = new List<int>();
			var b = new List<int>();
			
			var source = Observable.Range(1, 20);
			
			// var cache = new BlockingCollection<int>();
			
			// source.Do(i => cache.TryAdd(i)).Publish().RefCount().Subscribe();
			
			
			// var roundRobin = cache.GetConsumingEnumerable().ToObservable(Scheduler.Default);
			var roundRobin = source.RoundRobin().Publish().RefCount();
			
			roundRobin.Subscribe(i => a.Add(i));
			roundRobin.Subscribe(i => b.Add(i));

			// source.Connect();
			await Task.Delay(TimeSpan.FromSeconds(2));
			a.Count.ShouldBeGreaterThan(0);
			b.Count.ShouldBeGreaterThan(0);
			a.Concat(b).Count().ShouldBe(20);
			a.Intersect(b).Count().ShouldBe(0);

		}
		public static IObservable<T> Distribute<T>( IObservable<T> source) {
			var buffer = new BufferBlock<T>();
			source.Subscribe(buffer.AsObserver());             
			return Observable.Create<T>(observer => buffer.LinkTo(
				new ActionBlock<T>(observer.OnNext, new ExecutionDataflowBlockOptions { BoundedCapacity = 1 })
			));
		}

		[Test]
		public async Task Distribute() {
			await ObserveUnseenValues(source => source.Distribute());
		}

		private static async Task ObserveUnseenValues(Func<IObservable<int>,IObservable<int>> method) {
			var a = new List<int>();
			var b = new List<int>();
			
			var source = Observable.Range(1, 20).Publish();
			
			var roundRobin = method(source);
			
			roundRobin.Subscribe(i => a.Add(i));
			roundRobin.Subscribe(i => b.Add(i));

			source.Connect();
			await Task.Delay(TimeSpan.FromSeconds(2));
			a.Count.ShouldBeGreaterThan(0);
			b.Count.ShouldBeGreaterThan(0);
			a.Concat(b).Count().ShouldBe(20);
			a.Intersect(b).Count().ShouldBe(0);
		}


	}
}