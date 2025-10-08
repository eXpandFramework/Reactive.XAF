using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.Relay;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Tests.FaultHubTests.ResilienceModels {
    [TestFixture]
    public class EventStreamContextIsolationTests : FaultHubTestBase {
        public class MockEventSource {
            public event EventHandler<EventArgs> TestEvent;
            public void RaiseEvent() => TestEvent?.Invoke(this, EventArgs.Empty);
        }

        [Test]
        public async Task Event_Stream_Subscriptions_Should_Be_Diagnostically_Isolated() {
            var tcs = new TaskCompletionSource<FaultHubException>();
            using var busSubscription = FaultHub.Bus.Subscribe(tcs.SetResult);

            var eventSource = new MockEventSource();

            var streamA = eventSource.ProcessEvent<EventArgs,EventArgs>(nameof(MockEventSource.TestEvent), Observable.Return, context: ["Context_A"]);
            
            using var subscriptionA = streamA.Subscribe();

            var streamB = eventSource.ProcessEvent<EventArgs,EventArgs>(nameof(MockEventSource.TestEvent), _ => Observable.Throw<EventArgs>(new InvalidOperationException("Failure in B")), context: ["Context_B"]);

            using var subscriptionB = streamB.Subscribe(_ => { }, _ => { });
            eventSource.RaiseEvent();

            var fault = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(2));
            fault.ShouldNotBeNull();
            var allContexts = fault.AllContexts.ToArray();
            allContexts.ShouldContain("Context_B");
            allContexts.ShouldNotContain("Context_A",
                "Context leak detected: The context from a completely separate subscription ('Context_A') has polluted the error report of the failing operation ('Context_B').");
        }
    }
}