using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Tests.FaultHubTests.POC{
    [TestFixture]
    public class LogicalStackResetPoc : FaultHubTestBase {
        [Test]
        public async Task ChainFaultContext_Should_Reset_Upstream_Stack_While_Preserving_Internal_Stack() {

            var operationStream = Observable.Return(Unit.Default)
                .PushStackFrame("DirtyFrame_From_Previous_Unrelated_Work")
                .SelectMany(_ =>
                    Observable.Throw<Unit>(new InvalidOperationException("Inner Failure"))
                        .PushStackFrame("CleanFrame_From_Current_Work")
                        .ChainFaultContext(["MyIsolatedStory"])
                );


            await operationStream.PublishFaults().Capture();

            BusEvents.Count.ShouldBe(1);
            var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            var logicalStack = fault.LogicalStackTrace.ToArray();

            fault.AllContexts.ShouldContain("MyIsolatedStory");

            logicalStack.ShouldNotContain(frame => frame.MemberName == "DirtyFrame_From_Previous_Unrelated_Work",
                "The logical stack was not reset and contains frames from a previous context.");

            logicalStack.ShouldContain(frame => frame.MemberName == "CleanFrame_From_Current_Work",
                "The logical stack should contain the frames from within the new operation's boundary.");
        }
        
    }
}