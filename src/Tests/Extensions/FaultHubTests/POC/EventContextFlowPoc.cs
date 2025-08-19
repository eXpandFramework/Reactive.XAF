using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;
using Xpand.Extensions.Reactive.Transform;


namespace Xpand.Extensions.Tests.FaultHubTests.POC {
    [TestFixture]
    public class EventContextFlowPoc {
        // A simple, standard .NET event source.
        private class EventSource {
            public event EventHandler MyEvent;
            public void FireEvent() => MyEvent?.Invoke(this, EventArgs.Empty);
        }

        // The AsyncLocal variable to track context.
        private static readonly AsyncLocal<string> TestContext = new();

        [Test]
        public async Task FromEventPattern_Does_Not_Capture_AsyncLocal_Context_At_Subscription_Time() {
            
            var eventSource = new EventSource();
            string contextInsideHandler = "NOT_SET";
            
            var stream = Observable.FromEventPattern(
                handler => eventSource.MyEvent += handler,
                handler => eventSource.MyEvent -= handler
            );

            
            // 1. Set the initial context. This is the value we hope would be captured.
            TestContext.Value = "VALUE_AT_SUBSCRIBE_TIME";

            using var subscription = stream.Subscribe(_ => {
                // 4. When the event fires, this code executes and captures the CURRENT context.
                contextInsideHandler = TestContext.Value;
            });

            // 2. Immediately change the context *after* the subscription is made.
            TestContext.Value = "VALUE_AT_FIRE_TIME";

            // 3. Fire the event.
            eventSource.FireEvent();
            
            await Task.Delay(10); // Ensures any potential asynchronous operations complete.

            // ASSERT
            // This assertion proves that the context captured inside the handler was the one
            // present when the event was fired, not the one from when the subscription was created.
            contextInsideHandler.ShouldBe("VALUE_AT_FIRE_TIME");
            contextInsideHandler.ShouldNotBe("VALUE_AT_SUBSCRIBE_TIME");
        }
        

        [Test]
        public void FlowFaultContext_Ensures_Event_Streams_Use_Subscription_Context() {
            
            var eventSource = new EventSource();
            FaultHubException capturedFault = null;
            
            // 1. Set the initial context at the subscription site.
            FaultHub.LogicalStackContext.Value = [new LogicalStackFrame("SubscriptionContext", "", 0)];

            var stream = eventSource.WhenEvent(nameof(EventSource.MyEvent))
                .SelectMany(_ => Observable.Throw<System.Reactive.Unit>(new Exception("test")))
                .PushStackFrame("HandlerContext"); // This frame should be prepended to the subscription context.

            
            using (stream.Subscribe(_ => { }, ex => capturedFault = ex.ExceptionToPublish(
                       new object[]{}.NewFaultContext(FaultHub.LogicalStackContext.Value,"Boundary")))) {
                
                // 2. Change the context on the thread that will fire the event.
                FaultHub.LogicalStackContext.Value = [new LogicalStackFrame("FireEventContext", "", 0)];
                eventSource.FireEvent();
            }

            // ASSERT
            capturedFault.ShouldNotBeNull();
            var logicalStack = capturedFault.LogicalStackTrace.ToList();

            // 3. This assertion is expected to FAIL. It proves that the "SubscriptionContext" was lost
            // and the stack was built using the "FireEventContext" instead.
            logicalStack.ShouldNotContain(frame => frame.MemberName == "SubscriptionContext");
            logicalStack.ShouldContain(frame => frame.MemberName == "FireEventContext");
            logicalStack.First().MemberName.ShouldBe("FireEventContext");
        }
    }
    }
