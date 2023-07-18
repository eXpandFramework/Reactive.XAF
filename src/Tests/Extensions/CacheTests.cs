using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.Utility;
using Xpand.TestsLib;

namespace Xpand.Extensions.Tests {
    
    public class CacheTests:BaseTest {
        private IObservable<int> _source;
        private ConcurrentDictionary<string, IConnectableObservable<object>> _storage;
        private Func<int, IObservable<string>> _secondSelector;
        private TimeSpan _interval;

        public override void Setup() {
            base.Setup();
            _source = Observable.Range(1, 5);
            _storage = new ConcurrentDictionary<string, IConnectableObservable<object>>();
            _secondSelector = x => Observable.Return(x.ToString());
            _interval = TimeSpan.FromMilliseconds(500);
        }
        
        [Test]
        public void Should_Return_Values_From_Source_When_Interval_Is_Null() {
            var result = _source.Cache(_storage, "key", _secondSelector, null).ToList().Wait();

            result.Count.ShouldBe(5);
            int i;
            result.ShouldAllBe(x => int.TryParse(x,out i));
        }

        [Test]
        public void Should_Cache_Values_When_Interval_Is_Not_Null() {
            var result = _source.Cache(_storage, "key", _secondSelector, _interval).ToList().Wait();

            result.Count.ShouldBe(1);
            int i;
            result.ShouldAllBe(x => int.TryParse(x,out i));
            _storage.ContainsKey("key").ShouldBeTrue();
        }

        [Test]
        public void Should_Return_Cached_Values_When_They_Exist() {
            var cachedObservable = Observable.Return((object)"cached").Replay(1);
            var connection = cachedObservable.Connect();
            _storage.TryAdd("key", cachedObservable);

            var result = _source.Cache(_storage, "key", _secondSelector, _interval).ToList().Wait();

            connection.Dispose();

            result.Count.ShouldBe(1);
            result[0].ShouldBe("cached");
        }
        
    [Test]
    public void Should_Handle_Different_Types_Of_Observables() {
        _source = Observable.Interval(TimeSpan.FromMilliseconds(100)).Take(5).Select(l => (int)l);
        _secondSelector = x => Observable.Timer(TimeSpan.FromMilliseconds(100)).Select(_ => x.ToString());

        var result = _source.Cache(_storage, "key", _secondSelector, _interval).ToList().Wait();

        result.Count.ShouldBe(1);
        int i;
        result.ShouldAllBe(x => int.TryParse(x,out i));
    }

    [Test]
    public void Should_Handle_Different_Intervals() {
        _interval = TimeSpan.Zero;

        var result = _source.Cache(_storage, "key", _secondSelector, _interval).ToList().Wait();

        result.Count.ShouldBe(1);
        int i;
        result.ShouldAllBe(x => int.TryParse(x, out i));
    }

    [Test]
    public void Should_Handle_Different_Keys() {
        var result1 = _source.Cache(_storage, "key1", _secondSelector, _interval).ToList().Wait();
        var result2 = _source.Cache(_storage, "key2", _secondSelector, _interval).ToList().Wait();

        result1.Count.ShouldBe(1);
        int result;
        result1.ShouldAllBe(x => int.TryParse(x, out result));
        result2.Count.ShouldBe(1);
        int i;
        result2.ShouldAllBe(x => int.TryParse(x, out i));
        _storage.ContainsKey("key1").ShouldBeTrue();
        _storage.ContainsKey("key2").ShouldBeTrue();
    }

    [Test]
    public void Should_Handle_Different_Second_Selectors() {
        _secondSelector = _ => Observable.Throw<string>(new Exception("Test exception"));

        Should.Throw<Exception>(() => _source.Cache(_storage, "key", _secondSelector, _interval).ToList().Wait());
    }

    [Test]
    public void Should_Handle_Errors_From_Source_Observable() {
        _source = Observable.Throw<int>(new Exception("Test exception"));

        Should.Throw<Exception>(() => _source.Cache(_storage, "key", _secondSelector, _interval).ToList().Wait());
    }

    [Test]
    [SuppressMessage("ReSharper", "HeapView.CanAvoidClosure")]
    public void Should_Dispose_Connection_After_Completion() {
        var wasDisposed = false;
        _source = Observable.Create<int>(observer => {
            observer.OnNext(1);
            observer.OnCompleted();
            return Disposable.Create(() => wasDisposed = true);
        });

        _source.Cache(_storage, "key", _secondSelector, _interval).Subscribe();

        wasDisposed.ShouldBeTrue();
    }
    
    [Test]
    public void Should_Emit_Immediately_When_Cache_Is_Empty()
    {
        _interval = TimeSpan.FromMilliseconds(500);

        var stopwatch = Stopwatch.StartNew();

        var result = _source.Cache(_storage, "key", _secondSelector, _interval).ToList().Wait();

        stopwatch.Stop();

        result.Count.ShouldBe(1);
        int i;
        result.ShouldAllBe(x => int.TryParse(x, out i));
        stopwatch.ElapsedMilliseconds.ShouldBeLessThan(500);
    }

    }
}