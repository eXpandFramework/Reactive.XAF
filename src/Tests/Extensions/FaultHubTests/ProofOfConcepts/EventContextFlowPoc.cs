using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.Relay;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.Extensions.Tests.FaultHubTests.ProofOfConcepts {
    [TestFixture]
    public class EventContextFlowPoc {
        
        private class EventSource {
            public event EventHandler MyEvent;
            public void FireEvent() => MyEvent?.Invoke(this, EventArgs.Empty);
        }

        
        private static readonly AsyncLocal<string> TestContext = new();

        [Test]
        public async Task FromEventPattern_Does_Not_Capture_AsyncLocal_Context_At_Subscription_Time() {
            
            var eventSource = new EventSource();
            string contextInsideHandler = "NOT_SET";
            
            var stream = Observable.FromEventPattern(
                handler => eventSource.MyEvent += handler,
                handler => eventSource.MyEvent -= handler
            );

            
            
            TestContext.Value = "VALUE_AT_SUBSCRIBE_TIME";

            using var subscription = stream.Subscribe(_ => {
                
                contextInsideHandler = TestContext.Value;
            });

            
            TestContext.Value = "VALUE_AT_FIRE_TIME";

            
            eventSource.FireEvent();
            
            await Task.Delay(10); 

            
            
            
            contextInsideHandler.ShouldBe("VALUE_AT_FIRE_TIME");
            contextInsideHandler.ShouldNotBe("VALUE_AT_SUBSCRIBE_TIME");
        }
        

    }
    }
