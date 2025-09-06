using System;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Tests.FaultHubTests.ResilienceModels{
    public class CoreResilienceTests : FaultHubTestBase {

        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<int> GetFailingAsyncStreamWithResilience() {
            return Observable.Timer(TimeSpan.FromMilliseconds(50))
                .SelectMany(_ => Observable.Throw<int>(new InvalidOperationException("Async Core Failure")))
                .ContinueOnFault(context:["CoreContext"]);
        }

        [Test]
        public async Task Core_Resilience_Captures_Correct_Async_StackTrace() {
            var stream = GetFailingAsyncStreamWithResilience();

            await stream.PublishFaults().Capture();

            BusEvents.Count.ShouldBe(1, "The exception was not published to the FaultHub bus.");

            var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();

            
            fault.LogicalStackTrace.ShouldContain(frame => frame.MemberName == nameof(GetFailingAsyncStreamWithResilience));
            
        }        
        
    }
}