using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Tests.FaultHubTests._2_ResilienceModels {
    [TestFixture]
    public class Resilience_ContinueOnFault_Tests  : FaultHubTestBase {
        [Test]
        public async Task Suppresses_Error_Publishes_To_Bus_And_Captures_Caller_Context_Synchronously() {
            var source = Observable.Throw<int>(new InvalidOperationException("Sync Failure"));

            var result = await source.ContinueOnFault(context: ["MyContext"]).Capture();

            result.Error.ShouldBeNull();
            result.IsCompleted.ShouldBeTrue();

            BusEvents.Count.ShouldBe(1);
            var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            fault.InnerException.ShouldBeOfType<InvalidOperationException>();
            fault.LogicalStackTrace.First().MemberName.ShouldBe(nameof(Suppresses_Error_Publishes_To_Bus_And_Captures_Caller_Context_Synchronously));
            fault.AllContexts.ShouldContain("MyContext");
        }

        [Test]
        public async Task Suppresses_Error_And_Publishes_To_Bus_Asynchronously() {
            var source = Observable.Timer(TimeSpan.FromMilliseconds(20))
                .SelectMany(_ => Observable.Throw<int>(new InvalidOperationException("Async Failure")));

            var result = await source.ContinueOnFault().Capture();
            
            result.Error.ShouldBeNull();
            result.IsCompleted.ShouldBeTrue();
            BusEvents.Count.ShouldBe(1);
            BusEvents.Single().InnerException.ShouldBeOfType<InvalidOperationException>();
        }

        [Test]
        public async Task Works_With_Retry_Strategy_And_Publishes_Final_Error() {
            var attemptCount = 0;
            var source = Observable.Defer(() => {
                attemptCount++;
                return Observable.Throw<Unit>(new InvalidOperationException("Transient Error"));
            });

            await source.ContinueOnFault(s => s.Retry(3)).Capture();
            
            attemptCount.ShouldBe(3);
            BusEvents.Count.ShouldBe(1);
            BusEvents.Single().InnerException.ShouldBeOfType<InvalidOperationException>();
        }

        [Test]
        public async Task Handles_Exception_From_Upstream_Disposal() {
            var resource = new TestResource { OnDispose = () => throw new InvalidOperationException("Dispose failed.") };
            var sourceWithFailingDispose = Observable.Using(() => resource, _ => Observable.Return(42));
            
            var result = await sourceWithFailingDispose.ContinueOnFault().Capture();
            
            result.Items.ShouldBe([42]);
            result.IsCompleted.ShouldBeTrue();
            
            BusEvents.Count.ShouldBe(1);
            BusEvents.Single().InnerException.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Dispose failed.");
        }
    }
}