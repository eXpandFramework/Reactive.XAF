using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using akarnokd.reactive_extensions;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.ErrorHandling;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.Extensions.Tests.FaultHubTests{
    [TestFixture]
    public class ProcessEventTest : FaultHubTestBase {
        
        private class TestEventSource {
            public event EventHandler<EventArgs> MyEvent;

            public void RaiseEvent() {
                MyEvent?.Invoke(this, EventArgs.Empty);
            }
        }

        [Test]
        public void ProceedEvent_Survives_Error_And_Continues() {
            
            var eventSource = new TestEventSource();
            var eventCounter = 0;
            var hasThrown = false;

            
            using var _ = eventSource.ProcessEvent<EventArgs>(nameof(TestEventSource.MyEvent),e => e.Observe().Do(_ => {
                        Console.WriteLine($"{nameof(ProceedEvent_Survives_Error_And_Continues)} {nameof(eventCounter)}={eventCounter} {nameof(hasThrown)}={hasThrown})");
                        eventCounter++;
                        if (!hasThrown) {
                            hasThrown = true;
                            throw new InvalidOperationException("Handler failed");
                        }
                    })
                    .ToUnit())
                .PublishFaults().Test();
            
            eventSource.RaiseEvent(); 
            eventSource.RaiseEvent(); 

            
            BusObserver.ItemCount.ShouldBe(1);
            BusObserver.Items.First().InnerException.ShouldBeOfType<InvalidOperationException>();
            
            
            eventCounter.ShouldBe(2);

        }

        [Test]
        public void ProcessEvent_With_Unit_Return_Type_Does_Not_Emit_OnNext() {
            var eventSource = new TestEventSource();
            var selectWasCalled = false;
            
            var stream = eventSource.ProcessEvent(nameof(TestEventSource.MyEvent), _ => Observable.Return(Unit.Default));
            
            using var _ = stream.Select(_ => {
                selectWasCalled = true;
                throw new InvalidOperationException("This should never be thrown.");
#pragma warning disable CS0162 // Unreachable code detected
                return Unit.Default.Observe();
#pragma warning restore CS0162 // Unreachable code detected
            }).Subscribe();

            
            Should.NotThrow(() => eventSource.RaiseEvent());
            
            selectWasCalled.ShouldBeFalse();
        }   
        
        [Test]
        public void ProcessEvent_DetachesHandler_On_Disposal() {
            var eventSource = new TestEventSource();
            var eventCounter = 0;

            
            var subscription = eventSource.ProcessEvent<EventArgs>(nameof(TestEventSource.MyEvent), _ => {
                eventCounter++;
                return Observable.Return(Unit.Default);
            }).Test();


            eventSource.RaiseEvent();
            eventCounter.ShouldBe(1);

            
            subscription.Dispose();
            eventSource.RaiseEvent();

            
            eventCounter.ShouldBe(1);
        }
        
        [Test]
        public async Task ProcessEvent_Survives_Asynchronous_Error_And_Continues() {
            var eventSource = new TestEventSource();
            var eventCounter = 0;
            var hasThrown = false;

            using var _ = eventSource.ProcessEvent<EventArgs>(nameof(TestEventSource.MyEvent), _ => {
                eventCounter++;
                if (!hasThrown) {
                    hasThrown = true;
                    return Observable.Timer(TimeSpan.FromMilliseconds(50))
                        .SelectMany(_ => Observable.Throw<Unit>(new InvalidOperationException("Handler failed asynchronously")));
                }
                return Observable.Return(Unit.Default);
            }).PublishFaults().Test();
            
            eventSource.RaiseEvent(); 
            eventSource.RaiseEvent(); 
            
            await Task.Delay(100);
            
            BusObserver.ItemCount.ShouldBe(1);
            BusObserver.Items.First().InnerException.ShouldBeOfType<InvalidOperationException>();
            eventCounter.ShouldBe(2);
        }
        
        [Test]
        public void ProcessEvent_Throws_On_Subscription_For_NonExistent_Event() {
            var eventSource = new TestEventSource();
            

            Should.Throw<ArgumentException>(() => eventSource.ProcessEvent<EventArgs>("NonExistentEvent", _ => Observable.Return(Unit.Default)));
        }
    }
}