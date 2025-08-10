using System;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using akarnokd.reactive_extensions;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Numeric;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;
using Xpand.TestsLib.Common;

namespace Xpand.Extensions.Tests.FaultHubTests.POC{
    public class CoreResilienceTests : FaultHubTestBase {

        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<int> GetFailingAsyncStreamWithResilience() {
            // This method defines an observable that fails on a timer thread
            // and wraps it with ContinueOnFault, which uses the resilience
            // logic we are trying to fix.
            return Observable.Timer(TimeSpan.FromMilliseconds(50))
                .SelectMany(_ => Observable.Throw<int>(new InvalidOperationException("Async Core Failure")))
                .ContinueOnFault(["CoreContext"]);
        }

        [Test]
        public async Task Core_Resilience_Captures_Correct_Async_StackTrace() {
            // This test calls the helper and subscribes.
            var stream = GetFailingAsyncStreamWithResilience();
            
            using var testObserver = stream.PublishFaults().Test();

            await testObserver.AwaitDoneAsync(2.Seconds());

            // 1. Assert that the error was actually published to the bus.
            BusObserver.ItemCount.ShouldBe(1, "The exception was not published to the FaultHub bus.");
            
            var fault = BusObserver.Items.Single().ShouldBeOfType<FaultHubException>();
            var output = fault.ToString();
            
            // 2. Assert that the captured stack trace contains the name of the method
            //    where the resilient stream was defined.
            var expectedPattern = $@"(?s)--- Invocation Stack ---.*{nameof(GetFailingAsyncStreamWithResilience)}";
            output.ShouldMatch(expectedPattern, "The stack trace did not contain the calling method.");
        }
    }
}