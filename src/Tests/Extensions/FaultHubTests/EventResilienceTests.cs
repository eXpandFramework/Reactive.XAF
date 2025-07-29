using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using akarnokd.reactive_extensions;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Numeric;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Tests.FaultHubTests {
    public class EventResilienceTests : FaultHubTestBase {
        private class TestEventSource {
            public event EventHandler<MyEventArgs> MyEvent;
            private int _callCount;

            public void RaiseEvent() {
                _callCount++;
                MyEvent?.Invoke(this, new MyEventArgs { Value = $"Call {_callCount}" });
            }
        }

        private class MyEventArgs : EventArgs {
            public string Value { get; init; }
        }

        [Test]
        public void Chained_resilient_query_survives_error() {
            var source = new TestEventSource();
            var successfulProcessingCount = 0;
            var hasThrown = false;
            
            using var _ = source.ProcessEvent<MyEventArgs>(nameof(TestEventSource.MyEvent))
                .Where(e => e.Value != "ignore")
                .Select(e => {
                    if (hasThrown) return e.Value.ToUpper();
                    hasThrown = true;
                    throw new InvalidOperationException("Error in Select");
                })
                .Do(_ => successfulProcessingCount++)
                .Subscribe();

            
            source.RaiseEvent(); 
            source.RaiseEvent(); 
            source.RaiseEvent(); 

            
            BusObserver.ItemCount.ShouldBe(1);
            BusObserver.Items.Single().InnerException.ShouldBeOfType<InvalidOperationException>();
            
            successfulProcessingCount.ShouldBe(2); 
        }

        [Test]
        public void SelectResilient_Continues_Stream_After_Error() {
            var source = new[] { 1, 2, 3, 4 }.ToObservable();
            var results = new List<int>();
            
            source.SelectResilient(num => {
                    if (num == 2) {
                        throw new InvalidOperationException("This is a test error.");
                    }
                    return num * 10;
                })
                .Subscribe(results.Add);
            
            results.ShouldBe(new[] { 10, 30, 40 });

            
            BusObserver.ItemCount.ShouldBe(1);
            BusObserver.Items.Single().InnerException.ShouldBeOfType<InvalidOperationException>();
        }
        [Test]
        public void SelectManySequential_survives_error() {
            using var testObserver = new TestObserver<Unit>();
            using var observer = 1.Range(3).ToNowObservable()
                .Do(_ => testObserver.OnNext(Unit.Default))
                .SelectManySequential(_ => Observable.Throw<int>(new Exception())).Test();
            
            observer.ErrorCount.ShouldBe(0);
            testObserver.ItemCount.ShouldBe(3);
            BusObserver.ItemCount.ShouldBe(3);
        }
        [Test]
        public void Defer_survives_error() {
            using var testObserver = new TestObserver<Unit>();
            using var observer = testObserver.Defer(() => {
                testObserver.OnNext(Unit.Default);
                return Observable.Throw<int>(new Exception());
            }).Test();
            
            observer.ErrorCount.ShouldBe(0);
            testObserver.ItemCount.ShouldBe(1);
            BusObserver.ItemCount.ShouldBe(1);
        }

    }
    }
    

