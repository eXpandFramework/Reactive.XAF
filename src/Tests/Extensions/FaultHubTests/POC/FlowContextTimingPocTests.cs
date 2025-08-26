using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Tests.FaultHubTests.POC
{
    [TestFixture]
    public class FlowContextTimingPocTests
    {
        private static readonly AsyncLocal<string> TestContext = new();
        private string _capturedContextOnError;

        [SetUp]
        public void SetUp()
        {
            TestContext.Value = null;
            _capturedContextOnError = "CONTEXT_NOT_SET";
        }

        [Test]
        public async Task FlowContext_Captures_Context_At_Subscription_Time_Not_Emission_Time()
        {
            // 1. Set initial state, which should be ignored.
            TestContext.Value = "STATE_BEFORE_SUBSCRIPTION";

            var stream = Observable.Defer(() =>
            {
                // 2. Set the state that SHOULD be captured by FlowContext.
                // This code runs when the subscription travels up the chain.
                TestContext.Value = "STATE_AT_SUBSCRIPTION_TIME";

                var source = Observable.Timer(TimeSpan.FromMilliseconds(50))
                    .Do(_ =>
                    {
                        // 3. Set the state at the last moment before the error.
                        // This state should be overridden by the restored context from FlowContext.
                        TestContext.Value = "STATE_AT_EMISSION_TIME";
                    })
                    .SelectMany(_ => Observable.Throw<Unit>(new Exception("Error")));

                // 4. Apply FlowContext to the source, then add a probe to see the restored context.
                return source
                    .FlowContext(context:TestContext.Wrap()) // Captures "STATE_AT_SUBSCRIPTION_TIME"
                    .DoOnError(ex =>
                    {
                        // This probe runs with the restored context.
                        _capturedContextOnError = TestContext.Value;
                    });
            });

            // Await the stream. The .Catch prevents the exception from failing the test outright.
            await stream.Catch(Observable.Empty<Unit>());

            // 5. Assert that the context captured in the probe is the one from subscription time.
            _capturedContextOnError.ShouldBe("STATE_AT_SUBSCRIPTION_TIME");
        }
    }
}