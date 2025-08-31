using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Tests.Reactive.ProofOfConcepts {
    [TestFixture]
    public class FlowContextTimingPocTests {
        private static readonly AsyncLocal<string> TestContext = new();
        private string _capturedContextOnError;

        [SetUp]
        public void SetUp() {
            TestContext.Value = null;
            _capturedContextOnError = "CONTEXT_NOT_SET";
        }

        [Test]
        public async Task FlowContext_Captures_Context_At_Subscription_Time_Not_Emission_Time() {
            TestContext.Value = "STATE_BEFORE_SUBSCRIPTION";

            var stream = Observable.Defer(() => {
                TestContext.Value = "STATE_AT_SUBSCRIPTION_TIME";

                var source = Observable.Timer(TimeSpan.FromMilliseconds(50))
                    .Do(_ => { TestContext.Value = "STATE_AT_EMISSION_TIME"; })
                    .SelectMany(_ => Observable.Throw<Unit>(new Exception("Error")));

                return source
                    .FlowContext(context: TestContext.Wrap())
                    .DoOnError(_ => { _capturedContextOnError = TestContext.Value; });
            });

            await stream.Catch(Observable.Empty<Unit>()).LastOrDefaultAsync();

            _capturedContextOnError.ShouldBe("STATE_AT_SUBSCRIPTION_TIME");
        }
    }
}