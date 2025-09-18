using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Tests.FaultHubTests.Core {
    [TestFixture]
    public class NestedResilienceRenderingTests : FaultHubTestBase {
        // [Test][Ignore("not validated")]
        public void Renderer_Collapses_Redundant_Boundaries_Defined_In_Same_Method() {
            var streamWithSameMethodBoundaries = Observable.Defer(() => Observable.Throw<int>(new InvalidOperationException("Failure at Level 3"))
                    .PushStackFrame("Level3_Frame")
                    .PushStackFrame("Level2_Frame")
                    .ChainFaultContext(["Level2_Boundary"]))
                .PushStackFrame("Level1_Frame")
                .ChainFaultContext(["Level1_Boundary"])
                .ContinueOnFault();

            streamWithSameMethodBoundaries.Subscribe();

            BusEvents.Count.ShouldBe(1);
            var finalFault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            var report = finalFault.ToString();

            report.ShouldContain("Level1_Frame");
            report.ShouldContain("Level2_Frame");
            report.ShouldContain("Level3_Frame");
        }
        
        
        // [Test][Ignore("not validated")]
        public async Task Accurate_Replication_Of_Thin_Stack_From_Separate_Method_Boundaries() {
            await Level1HasOuterBoundaries().Capture();

            BusEvents.Count.ShouldBe(1);
            var finalFault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            var report = finalFault.ToString();

            report.ShouldNotContain("Level2_Frame");
            report.ShouldNotContain("Level3_Frame");
            report.ShouldContain("Level1_Frame");

            var aggregatedStack = finalFault.LogicalStackTrace.Select(f => f.MemberName).ToArray();
            aggregatedStack.ShouldContain("Level1_Frame");
            aggregatedStack.ShouldContain("Level2_Frame");
            aggregatedStack.ShouldContain("Level3_Frame");
            
            IObservable<int> Level3FailsWithFrame()
                => Observable.Throw<int>(new InvalidOperationException("Failure at Level 3"))
                    .PushStackFrame("Level3_Frame");

            IObservable<int> Level2HasFirstBoundary()
                => Level3FailsWithFrame()
                    .PushStackFrame("Level2_Frame")
                    .ChainFaultContext(["Level2_Boundary"]);
            IObservable<int> Level1HasOuterBoundaries()
                => Level2HasFirstBoundary()
                    .PushStackFrame("Level1_Frame")
                    .ChainFaultContext(["Level1_Boundary"])
                    .ContinueOnFault();
        }
    }
}