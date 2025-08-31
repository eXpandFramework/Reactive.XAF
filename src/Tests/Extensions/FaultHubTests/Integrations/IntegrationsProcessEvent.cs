using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.Extensions.Tests.FaultHubTests.Integrations {
    public class IntegrationsProcessEvent:FaultHubTestBase {
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

            using var _ = eventSource.ProcessEvent<EventArgs,Unit>(nameof(TestEventSource.MyEvent), e => e.Observe().Do(_ => {
                    Console.WriteLine($"{nameof(ProceedEvent_Survives_Error_And_Continues)} {nameof(eventCounter)}={eventCounter} {nameof(hasThrown)}={hasThrown})");
                    eventCounter++;
                    if (!hasThrown) {
                        hasThrown = true;
                        throw new InvalidOperationException("Handler failed");
                    }
                    })
                    .ToUnit())
                .PublishFaults().Subscribe();
            
            eventSource.RaiseEvent(); 
            eventSource.RaiseEvent(); 
            
            BusEvents.Count.ShouldBe(1);
            BusEvents.First().InnerException.ShouldBeOfType<InvalidOperationException>();
            
            eventCounter.ShouldBe(2);
        }

        [Test]
        public void ProcessEvent_DetachesHandler_On_Disposal() {
            var eventSource = new TestEventSource();
            var eventCounter = 0;

            var subscription = eventSource.ProcessEvent<EventArgs,Unit>(nameof(TestEventSource.MyEvent), _ => {
                eventCounter++;
                return Observable.Return(Unit.Default);
            }).Subscribe();

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

            using var _ = eventSource.ProcessEvent<EventArgs,Unit>(nameof(TestEventSource.MyEvent), _ => {
                eventCounter++;
                if (!hasThrown) {
                    hasThrown = true;
                    return Observable.Timer(TimeSpan.FromMilliseconds(50))
                        .SelectMany(_ => Observable.Throw<Unit>(new InvalidOperationException("Handler failed asynchronously")));
                }
                return Observable.Return(Unit.Default);
            }).PublishFaults().Subscribe();
            
            eventSource.RaiseEvent(); 
            eventSource.RaiseEvent(); 
            
            await Task.Delay(100);
            
            BusEvents.Count.ShouldBe(1);
            BusEvents.First().InnerException.ShouldBeOfType<InvalidOperationException>();
            eventCounter.ShouldBe(2);
        }
        
        [Test]
        public void ProcessEvent_Throws_On_Subscription_For_NonExistent_Event() {
            var eventSource = new TestEventSource();
            
            Should.Throw<ArgumentException>(() => eventSource.ProcessEvent<EventArgs,Unit>("NonExistentEvent", _ => Observable.Return(Unit.Default)));
        }
        private class EventSource {
            public event EventHandler MyEvent;
            public void FireEvent() => MyEvent?.Invoke(this, EventArgs.Empty);
        }

        [Test]
        public void ProcessEvent_Preserves_Subscription_Context_On_Event_Streams() {
            
            var eventSource = new EventSource();
    

            FaultHub.LogicalStackContext.Value = [new LogicalStackFrame("SubscriptionContext", "", 0)];

            var stream = eventSource.ProcessEvent<EventArgs, Unit>(nameof(EventSource.MyEvent),
                    _ => Observable.Throw<Unit>(new Exception("test")).PushStackFrame("HandlerContext"))
                ;

            
            
            using (stream.Subscribe()) {
                FaultHub.LogicalStackContext.Value = [new LogicalStackFrame("FireEventContext", "", 0)];
                eventSource.FireEvent();
            }
            
            FaultHub.LogicalStackContext.Value = null;

            
            BusEvents.Count.ShouldBe(1);
            var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            var logicalStack = fault.LogicalStackTrace.ToList();

            logicalStack.Count.ShouldBe(3);
            logicalStack[0].MemberName.ShouldBe(nameof(ProcessEvent_Preserves_Subscription_Context_On_Event_Streams));
            logicalStack[1].MemberName.ShouldBe("HandlerContext");
            logicalStack[2].MemberName.ShouldBe("SubscriptionContext");
        }

    }
}