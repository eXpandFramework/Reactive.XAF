using System;
using System.Linq;
using akarnokd.reactive_extensions;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Numeric;
using Xpand.Extensions.Reactive.ErrorHandling;
using Xpand.Extensions.Reactive.Transform;

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

            using var _=source.ProcessEvent<MyEventArgs>(nameof(TestEventSource.MyEvent))
                .Where(e => e.Value != "ignore")
                .Select(e => {
                    if (hasThrown) return e.Value.ToUpper();
                    hasThrown = true;
                    throw new InvalidOperationException("Error in Select");
                })
                .Do(_ => successfulProcessingCount++)
                .PublishFaults()
                .Test();

            
            source.RaiseEvent(); 
            source.RaiseEvent(); 
            source.RaiseEvent(); 

            
            BusObserver.ItemCount.ShouldBe(1);
            BusObserver.Items.Single().InnerException.ShouldBeOfType<InvalidOperationException>();
            
            successfulProcessingCount.ShouldBe(2); 
        }
        [Test]
        public void RethrowOnError_terminates_resilient_query_on_error() {
            var source = new TestEventSource();
            var successfulProcessingCount = 0;
            var hasThrown = false;
            
            var testObserver = source.ProcessEvent<MyEventArgs>(nameof(TestEventSource.MyEvent))
                .Where(e => e.Value != "ignore")
                .Select(e => {
                    if (hasThrown) return e.Value.ToUpper();
                    hasThrown = true;
                    throw new InvalidOperationException("Error in Select");
                })
                .Do(_ => successfulProcessingCount++)
                .RethrowOnError()
                .PublishFaults()
                .Test();

            
            source.RaiseEvent();
            source.RaiseEvent();
            source.RaiseEvent();

            testObserver.AwaitDone(300.Milliseconds());
            
            testObserver.ErrorCount.ShouldBe(1);
            testObserver.Errors.First().InnerException.ShouldBeOfType<InvalidOperationException>();
            BusObserver.ItemCount.ShouldBe(0);
            successfulProcessingCount.ShouldBe(0);
        }
        
    }
    }
    

