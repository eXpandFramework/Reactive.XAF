using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Numeric;
using Xpand.Extensions.Reactive.Relay;
using Xpand.Extensions.Reactive.Transform.System;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Tests.FaultHubTests.Core {
    [TestFixture]
    public class PushStackFrameTests : FaultHubTestBase {
        [Test]
        public void PushStackFrame_Correctly_Appends_To_Existing_Logical_Stack() {
            
            IReadOnlyList<LogicalStackFrame> stackAtErrorTime = null;
            var initialFrame = new LogicalStackFrame("OuterScope", "test.cs", 1);

            
            FaultHub.LogicalStackContext.Value = [initialFrame];
            var stream = Observable.Throw<Unit>(new Exception("test"))
                .PushStackFrame("InnerScope");
            stream.Catch((Exception _) => {
                stackAtErrorTime = FaultHub.LogicalStackContext.Value;
                return Observable.Empty<Unit>();
            }).Subscribe();
            FaultHub.LogicalStackContext.Value = null;

            
            stackAtErrorTime.ShouldNotBeNull("The logical stack was null at the time of the error.");
            var logicalStack = stackAtErrorTime.ToList();
            var outerFrameIndex = logicalStack.FindIndex(f => f.MemberName == "OuterScope");
            var innerFrameIndex = logicalStack.FindIndex(f => f.MemberName == "InnerScope");
            outerFrameIndex.ShouldNotBe(-1, "The outer frame was lost.");
            innerFrameIndex.ShouldNotBe(-1, "The inner frame was not added.");
            innerFrameIndex.ShouldBeLessThan(outerFrameIndex, "The inner frame should be prepended to the outer frame.");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> LowLevel_For_Boundary_Test()
            => Observable.Throw<Unit>(new InvalidOperationException("Failure")).PushStackFrame();
        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> MidLevel_For_Boundary_Test()
            => LowLevel_For_Boundary_Test().PushStackFrame();
        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> TopLevel_For_Boundary_Test()
            => MidLevel_For_Boundary_Test().PushStackFrame();
        [Test]
        public async Task PushStackFrame_Requires_ChainFaultContext_Boundary_To_Capture_Logical_Stack() {
            var streamWithoutBoundary = TopLevel_For_Boundary_Test().PublishFaults();
            await streamWithoutBoundary.Capture();

            BusEvents.Count.ShouldBe(1);
            var faultWithoutBoundary = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            var stackWithoutBoundary = faultWithoutBoundary.LogicalStackTrace.Select(f => f.MemberName).ToArray();
            stackWithoutBoundary.ShouldNotContain(nameof(TopLevel_For_Boundary_Test), "The stack from the nested calls was captured, but it should have been lost without a boundary.");
            stackWithoutBoundary.ShouldNotContain(nameof(MidLevel_For_Boundary_Test));
            stackWithoutBoundary.ShouldNotContain(nameof(LowLevel_For_Boundary_Test));
            Dispose();
            Setup();

            var streamWithBoundary = TopLevel_For_Boundary_Test()
                .ChainFaultContext(["BoundaryForTest"])
                .PublishFaults();
            await streamWithBoundary.Capture();

            BusEvents.Count.ShouldBe(1);
            var faultWithBoundary = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            var stackWithBoundary = faultWithBoundary.LogicalStackTrace.Select(f => f.MemberName).ToArray();
            
            stackWithBoundary.ShouldContain(nameof(TopLevel_For_Boundary_Test));
            stackWithBoundary.ShouldContain(nameof(MidLevel_For_Boundary_Test));
            stackWithBoundary.ShouldContain(nameof(LowLevel_For_Boundary_Test));
        }
        
        [Test]
        public async Task PushStackFrame_Prevents_Consecutive_Duplicate_Frames() {
            IObservable<Unit> RecursiveMethodWithSameFrame(int depth) {
                if (depth <= 0) {
                    return Observable.Throw<Unit>(new InvalidOperationException("Base case reached"));
                }

                return RecursiveMethodWithSameFrame(depth - 1)
                    .PushStackFrame(memberName: "RecursiveMethodWithSameFrame", filePath: "test.cs", lineNumber: 1, context: ["StaticContext"]);
            }

            var stream = RecursiveMethodWithSameFrame(2)
                .ChainFaultContext(["Boundary"])
                .PublishFaults();

            await stream.Capture();

            BusEvents.Count.ShouldBe(1);
            var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            var logicalStack = fault.LogicalStackTrace.ToList();

            logicalStack.Count(f => f.MemberName == "RecursiveMethodWithSameFrame").ShouldBe(1);
        }
    
        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> TopLevel_Operation()
            => MidLevel_Helper()
                .ChainFaultContext();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> MidLevel_Helper()
            => LowLevel_Error_Source().PushStackFrame();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> LowLevel_Error_Source()
            => 100.Milliseconds().Timer()
                .SelectMany(_ => Observable.Throw<Unit>(new InvalidOperationException("Failure at the lowest level.")))
                .PushStackFrame();

        [Test]
        public async Task Nested_PushStackFrame_Preserves_Correct_Order_On_Final_Report() {
            await TopLevel_Operation().PublishFaults().Capture();

            BusEvents.Count.ShouldBe(1);
            var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            var logicalStack = fault.LogicalStackTrace.Select(f => f.MemberName).ToArray();

            logicalStack.ShouldNotBeNull();

            var topLevelIndex = Array.IndexOf(logicalStack, nameof(TopLevel_Operation));
            var midLevelIndex = Array.IndexOf(logicalStack, nameof(MidLevel_Helper));
            var lowLevelIndex = Array.IndexOf(logicalStack, nameof(LowLevel_Error_Source));

            lowLevelIndex.ShouldBeLessThan(midLevelIndex,
                "The logical stack is not in the correct order: LowLevel should come before MidLevel.");
            midLevelIndex.ShouldBeLessThan(topLevelIndex,
                "The logical stack is not in the correct order: MidLevel should come before TopLevel.");
        }
    }
}