using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Tests.FaultHubTests.ResilienceModels {
    [TestFixture]
    public class SwitchOnFaultTests : FaultHubTestBase {
        [Test]
        public async Task FallbackSelector_Receives_Correctly_Contextualized_FaultHubException() {
            var source = Observable.Throw<string>(new InvalidOperationException("Inner Failure"));
            FaultHubException capturedFault = null;
            var context = new object[] { "SwitchContext" };

            var result = await source
                .PushStackFrame()
                .SwitchOnFault(fault => {
                    capturedFault = fault;
                    return Observable.Return("Fallback Value");
                }, context: context)
                .Capture();

            result.Error.ShouldBeNull();
            result.IsCompleted.ShouldBeTrue();
            result.Items.ShouldHaveSingleItem();
            result.Items.Single().ShouldBe("Fallback Value");
            
            BusEvents.ShouldBeEmpty();

            capturedFault.ShouldNotBeNull();
            capturedFault.InnerException.ShouldBeOfType<InvalidOperationException>()
                .Message.ShouldBe("Inner Failure");
            
            capturedFault.AllContexts.ShouldContain("SwitchContext");
            capturedFault.LogicalStackTrace.ShouldContain(f => f.MemberName == nameof(FallbackSelector_Receives_Correctly_Contextualized_FaultHubException));
        }
    }
}