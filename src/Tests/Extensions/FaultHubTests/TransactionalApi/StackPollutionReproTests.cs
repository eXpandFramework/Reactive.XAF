using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Reactive.Relay;
using Xpand.Extensions.Reactive.Relay.Transaction;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Tests.FaultHubTests.TransactionalApi {
    [TestFixture]
    public class StackPollutionReproTests : FaultHubTestBase {
        [Test]
        public async Task Rogue_Cleanup_Does_Not_Erase_Parent_Context_Between_Steps() {
            // ARRANGE
            // A nested transaction that fails.
            IObservable<Unit[]> NestedFailingTx() =>
                Observable.Return(Unit.Default)
                    .BeginWorkflow("NestedTx")
                    .Then(_ => Observable.Throw<Unit>(new InvalidOperationException("Inner Failure")))
                    .RunToEnd();

            // A second, independent step that also fails.
            IObservable<Unit> SecondFailingStep() =>
                Observable.Throw<Unit>(new InvalidOperationException("Outer Step 2 Failure"));

            // An outer transaction that executes the two failing steps sequentially.
            var transaction = Observable.Return(Unit.Default)
                .BeginWorkflow("OuterTx")
                .Then(_ => NestedFailingTx(), "Step1_With_Nested_Tx")
                .Then(_ => SecondFailingStep(), "Step2_Fails_Independently")
                .RunToEnd();

            // ACT
            await transaction.PublishFaults().Capture();

            // ASSERT
            BusEvents.Count.ShouldBe(1);
            var finalFault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            var aggregate = finalFault.InnerException.ShouldBeOfType<AggregateException>();
            
            // Isolate the fault specifically from the second step.
            var step2Fault = aggregate.InnerExceptions.OfType<FaultHubException>()
                .Single(ex => ex.InnerException is InvalidOperationException exi && exi.Message == "Outer Step 2 Failure");

            var frameNames = step2Fault.LogicalStackTrace.Select(f => f.MemberName).ToArray();

            // CRUCIAL ASSERTION: The stack for the second step MUST contain the frame from the parent transaction.
            // If this fails, it proves the context was incorrectly cleared between steps.
            frameNames.ShouldContain("OuterTx",
                "The parent transaction's context was cleared by the failure of the preceding step.");
        }
         [Test]
        public async Task Nested_Transaction_Cleanup_Isolates_Subsequent_Steps() {
            IObservable<Unit[]> NestedFailingTx() =>
                Observable.Return(Unit.Default)
                    .BeginWorkflow("NestedTx")
                    .Then(_ => Observable.Throw<Unit>(new InvalidOperationException("Inner Failure"))
                        .PushStackFrame("Frame_From_Inner_Failure"))
                    .RunToEnd();

            IObservable<Unit> SecondFailingStep() =>
                Observable.Throw<Unit>(new InvalidOperationException("Outer Step 2 Failure"))
                    .PushStackFrame("Frame_From_Step2");

            var transaction = Observable.Return(Unit.Default)
                .BeginWorkflow("OuterTx")
                .Then(_ => NestedFailingTx(), "Step1_With_Nested_Tx")
                .Then(_ => SecondFailingStep(), "Step2_Fails_Independently")
                .RunToEnd();

            await transaction.PublishFaults().Capture();

            BusEvents.Count.ShouldBe(1);
            var finalFault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            var aggregate = finalFault.InnerException.ShouldBeOfType<AggregateException>();

            var step2Fault = aggregate.InnerExceptions.OfType<FaultHubException>()
                .Single(ex => ex.InnerException is InvalidOperationException exi && exi.Message == "Outer Step 2 Failure");

            var logicalStack = step2Fault.LogicalStackTrace.ToList();
            var frameNames = logicalStack.Select(f => f.MemberName).ToArray();

            frameNames.ShouldNotContain("Frame_From_Inner_Failure", 
                "The stack from the first nested transaction has leaked into the subsequent step.");
            frameNames.ShouldNotContain("NestedTx", 
                "The stack from the first nested transaction has leaked into the subsequent step.");

            frameNames.ShouldContain("Frame_From_Step2");
            frameNames.ShouldContain("Step2_Fails_Independently");
        }
        
        [Test]
        public async Task Nested_Transaction_Boundary_Preserves_Parent_Step_Frame() {
            IObservable<Unit[]> NestedFailingTx() =>
                Observable.Return(Unit.Default)
                    .BeginWorkflow("NestedTx")
                    .Then(_ => Observable.Throw<Unit>(new InvalidOperationException("Inner Failure")))
                    .RunToEnd();

            var transaction = Observable.Return(Unit.Default)
                .BeginWorkflow("OuterTx")
                .Then(_ => NestedFailingTx(), "OuterStep") 
                .RunToEnd();

            await transaction.PublishFaults().Capture();

            BusEvents.Count.ShouldBe(1);
            var finalFault = BusEvents.Single().ShouldBeOfType<FaultHubException>();

            var logicalStack = finalFault.LogicalStackTrace.ToList();
            var frameNames = logicalStack.Select(f => f.MemberName).ToArray();

            frameNames.ShouldContain("OuterStep", "The stack frame from the parent step was erased by the nested transaction's resilience boundary.");
        }

        [Test]
        public void Union_Of_Nodes_With_Redundant_Names_Does_Not_Duplicate_Stack_Frames() {
            var rootCause = new InvalidOperationException("Root Cause");
            var childFrame = new LogicalStackFrame("Child_Method", "child.cs", 20);
            var ctxChild = new AmbientFaultContext { BoundaryName = "CommonName", LogicalStackTrace = new[] { childFrame }.ToImmutableStack() };
            var fhChild = new FaultHubException("Child Failed", rootCause, ctxChild);

            var parentFrame = new LogicalStackFrame("Parent_Method", "parent.cs", 10);
            var ctxParent = new AmbientFaultContext { BoundaryName = "CommonName", LogicalStackTrace = new[] { parentFrame }.ToImmutableStack(), InnerContext = ctxChild };
            var exception = new FaultHubException("Parent Failed", fhChild, ctxParent);

            var mergedNode = exception.OperationTree();

            mergedNode.ShouldNotBeNull();
            mergedNode.Name.ShouldBe("CommonName");

            var logicalStack = mergedNode.GetLogicalStack().ToList();

            logicalStack.Count.ShouldBe(2, "The merged stack should contain exactly two frames.");

            var frameNames = logicalStack.Select(f => f.MemberName).ToArray();
            frameNames.ShouldContain("Parent_Method");
            frameNames.ShouldContain("Child_Method");
        }
        [Test]
        public void OperationTree_Parser_Does_Not_Duplicate_Frames_In_Linear_Chain() {
            var rootCause = new InvalidOperationException("Root Cause");
            var frame3 = new LogicalStackFrame("Level3_Method", "level3.cs", 30);
            var ctx3 = new AmbientFaultContext { BoundaryName = "Level3", LogicalStackTrace = new[] { frame3 }.ToImmutableStack() };
            var fh3 = new FaultHubException("Level 3 Failed", rootCause, ctx3);

            var frame2 = new LogicalStackFrame("Level2_Method", "level2.cs", 20);
            var ctx2 = new AmbientFaultContext { BoundaryName = "Level2", LogicalStackTrace = new[] { frame2 }.ToImmutableStack(), InnerContext = ctx3 };
            var fh2 = new FaultHubException("Level 2 Failed", fh3, ctx2);

            var frame1 = new LogicalStackFrame("Level1_Method", "level1.cs", 10);
            var ctx1 = new AmbientFaultContext { BoundaryName = "Level1", LogicalStackTrace = new[] { frame1 }.ToImmutableStack(), InnerContext = ctx2 };
            var exception = new FaultHubException("Level 1 Failed", fh2, ctx1);

            var tree = exception.OperationTree();

            var level1Node = tree;
            var level2Node = level1Node.Children.Single();
            var level3Node = level2Node.Children.Single();

            var logicalStack = level3Node.GetLogicalStack().ToList();

            logicalStack.Count.ShouldBe(3, "The stack should contain exactly one frame from each level.");

            var distinctStack = logicalStack.Distinct().ToList();
            distinctStack.Count.ShouldBe(logicalStack.Count, "The stack should not contain duplicate frames.");

            var frameNames = logicalStack.Select(f => f.MemberName).ToArray();
            frameNames[0].ShouldBe("Level3_Method");
            frameNames[1].ShouldBe("Level2_Method");
            frameNames[2].ShouldBe("Level1_Method");
        }
    }
}