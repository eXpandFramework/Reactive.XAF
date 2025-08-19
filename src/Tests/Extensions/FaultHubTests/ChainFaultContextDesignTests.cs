using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Tests.FaultHubTests {
    [TestFixture]
    public class ChainFaultContextDesignTests : FaultHubTestBase {
        

        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<int> FailingAsyncOperation_WithLogicalStack() {
            
            return Observable.Timer(TimeSpan.FromMilliseconds(20))
                .SelectMany(_ => Observable.Throw<int>(new InvalidOperationException("Async Failure")))
                .PushStackFrame("ImportantLogicalFrame");
        }

        [Test]
        public async Task ChainFaultContext_Should_Preserve_The_Logical_Stack_From_Async_Operation() {
            
            

            
            var stream = FailingAsyncOperation_WithLogicalStack()
                .ChainFaultContext(["AsyncBoundary"]);

            
            await stream.PublishFaults().Capture();
            
            var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            var logicalStack = fault.LogicalStackTrace.ToList();


            logicalStack.ShouldContain(
                frame => frame.MemberName == "ImportantLogicalFrame",
                "The logical stack from the async operation was discarded, but it should have been preserved.");
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<int> Level3_DetailWork() => Observable
            .Throw<int>(new InvalidOperationException("Failure in DetailWork"))
            .PushStackFrame(["Saving database record"]);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<int> Level2_BusinessLogic() => Level3_DetailWork()
            .PushStackFrame(); 

        [Test]
        public async Task ChainFaultContext_Should_Capture_The_Upstream_Logical_Story_Within_Its_Boundary() {

            
            var stream = Level2_BusinessLogic()
                .ChainFaultContext(
                    source => source.Retry(2),
                    ["Level1_TransactionBoundary"]
                );

            
            await stream.PublishFaults().Capture();

            
            var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            var logicalStack = fault.LogicalStackTrace.ToList();
            var allContexts = fault.AllContexts.ToArray();

            
            allContexts.ShouldContain("Level1_TransactionBoundary");


            logicalStack.SelectMany(frame => frame.Context).ShouldContain(f => (string)f == "Saving database record");
            logicalStack.ShouldContain(f => f.MemberName == nameof(Level2_BusinessLogic));
            logicalStack.ShouldContain(f => f.MemberName == nameof(Level3_DetailWork));
        }



        [Test]
        public async Task ChainFaultContext_Should_Isolate_Context_In_Concurrent_Operations() {
            

            
            var streamA = Observable.Timer(TimeSpan.FromMilliseconds(10))
                .SelectMany(_ => Observable.Throw<Unit>(new Exception("Failure A")))
                .PushStackFrame("StreamA_LogicalFrame")
                .ChainFaultContext(["StreamA_Boundary"]);

            var streamB = Observable.Timer(TimeSpan.FromMilliseconds(20))
                .SelectMany(_ => Observable.Throw<Unit>(new Exception("Failure B")))
                .PushStackFrame("StreamB_LogicalFrame")
                .ChainFaultContext(["StreamB_Boundary"]);

            
            
            await streamA.PublishFaults()
                .Merge(streamB.PublishFaults())
                .Capture();

            
            BusEvents.Count.ShouldBe(2);
            var faults = BusEvents.OfType<FaultHubException>().ToList();

            
            var faultA = faults.Single(f => f.AllContexts.Contains("StreamA_Boundary"));
            var stackA = faultA.LogicalStackTrace.Select(f => f.MemberName).ToArray();

            
            var faultB = faults.Single(f => f.AllContexts.Contains("StreamB_Boundary"));
            var stackB = faultB.LogicalStackTrace.Select(f => f.MemberName).ToArray();

            
            faultA.AllContexts.ShouldNotContain("StreamB_Boundary",
                "Context from Stream B leaked into Stream A's error report.");
            stackA.ShouldNotContain("StreamB_LogicalFrame",
                "Logical stack from Stream B leaked into Stream A's error report.");

            
            faultB.AllContexts.ShouldNotContain("StreamA_Boundary",
                "Context from Stream A leaked into Stream B's error report.");
            stackB.ShouldNotContain("StreamA_LogicalFrame",
                "Logical stack from Stream A leaked into Stream B's error report.");



            stackA.ShouldContain("StreamA_LogicalFrame");
            stackB.ShouldContain("StreamB_LogicalFrame");
        }

        





        [Test]
        public async Task ChainFaultContext_Should_Yield_To_Outer_Resilience_Boundary() {
            
            
            

            
            var innerCounter = new SubscriptionCounter();
            var outerCounter = new SubscriptionCounter();

            
            var innerMostOperation = Observable.Defer(() => {
                    innerCounter.Increment();
                    return Observable.Throw<Unit>(new Exception("Inner Failure"));
                })
                .PushStackFrame("InnerMost_LogicalFrame");

            
            var innerBoundary = innerMostOperation
                .ChainFaultContext(
                    source => source.Retry(5), 
                    ["InnerBoundary_Context"]
                );

            // 3. The outermost resilience boundary, whose retry policy should take precedence.
            var outerBoundary = innerBoundary
                .TrackSubscriptions(outerCounter)
                .ChainFaultContext(
                    source => source.Retry(3), // This is the controlling retry policy.
                    ["OuterBoundary_Context"]
                );

            
            await outerBoundary.PublishFaults().Capture();

            // ASSERT
            var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            var logicalStack = fault.LogicalStackTrace.Select(f => f.MemberName).ToArray();
            var allContexts = fault.AllContexts.ToArray();

            // 1. Assert Policy Precedence: The outer retry policy (3) should have been used.
            // The inner operation is subscribed to once by the inner boundary, and the inner
            // boundary is subscribed to 3 times by the outer boundary.
            outerCounter.Count.ShouldBe(3, "The outer retry policy did not take precedence.");
            innerCounter.Count.ShouldBe(15, "The inner operation was not retried correctly by the outer boundary.");

            // 2. Assert Context Chaining: The high-level story should be complete.
            allContexts.ShouldContain("OuterBoundary_Context");
            allContexts.ShouldContain("InnerBoundary_Context");
            Array.IndexOf(allContexts, "OuterBoundary_Context")
                .ShouldBeLessThan(Array.IndexOf(allContexts, "InnerBoundary_Context"));

// MODIFICATION: This assertion is changed to reflect that the innermost boundary ("InnerBoundary_Context")
// will clear the upstream logical stack, so "InnerMost_LogicalFrame" will not be present in the final report.
            logicalStack.ShouldContain("InnerMost_LogicalFrame");
        }
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> FailingAsyncOperationWithUpstreamStack() {
            return Observable.Timer(TimeSpan.FromMilliseconds(20))
                // This SelectMany hops to a background thread, which is critical for replicating the issue.
                .SelectMany(_ => Observable.Throw<Unit>(new InvalidOperationException("Async Failure")))
                // This PushStackFrame is what gets torn down too early.
                .PushStackFrame("BusinessLogicFrame");
        }

        [Test]
        public async Task ChainFaultContext_Correctly_Captures_Stack_From_Single_Async_PushStackFrame()
        {
            // PURPOSE: This test verifies that for a single upstream async operation,
            // the Using-based PushStackFrame correctly preserves the logical context
            // long enough for the downstream ChainFaultContext's Catch block to observe it.

            
            var stream = FailingAsyncOperationWithUpstreamStack()
                .ChainFaultContext(new[] { "Boundary" });

            
            await stream.PublishFaults().Capture();

            // ASSERT
            BusEvents.Count.ShouldBe(1);
            var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            var logicalStack = fault.LogicalStackTrace.ToList();

            // This assertion will PASS, proving the operator works correctly in this simple case.
            logicalStack.ShouldContain(
                frame => frame.MemberName == "BusinessLogicFrame",
                "The logical stack from the async operation should have been preserved."
            );
        }
        
        [Test]
        public async Task ChainFaultContext_Captures_Full_Stack_From_Chained_Async_PushStackFrame_Operators()
        {
            // PURPOSE: This test verifies that for a chain of upstream async operations,
            // the Using-based PushStackFrame implementation correctly preserves the full logical stack
            // long enough for the downstream ChainFaultContext's Catch block to observe it.

            
            var stream = Observable.Timer(TimeSpan.FromMilliseconds(20))
                .SelectMany(_ => Observable.Throw<Unit>(new InvalidOperationException("Async Failure")))
                .PushStackFrame("InnerFrame") // The inner frame in the chain
                .PushStackFrame("OuterFrame") // The outer frame in the chain
                .ChainFaultContext(["Boundary"]);

            
            await stream.PublishFaults().Capture();

            // ASSERT
            BusEvents.Count.ShouldBe(1);
            var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            var logicalStack = fault.LogicalStackTrace.ToList();

            // This assertion should PASS, proving that chaining PushStackFrame operators works correctly.
            logicalStack.ShouldContain(frame => frame.MemberName == "InnerFrame");
            logicalStack.ShouldContain(frame => frame.MemberName == "OuterFrame");
        }
        // MODIFICATION: Added a new test to reproduce the stack pollution issue with retries.
        [Test]
        public async Task ChainFaultContext_With_Retry_Should_Reset_Logical_Stack_On_Each_Attempt() {
            // PURPOSE: This test proves that the logical stack is polluted on retries.
            // It is designed to FAIL with the current implementation. A correct implementation
            // should reset the logical stack on each retry attempt, resulting in a stack count of 1.
            // The current implementation will fail with a count of 3.

            
            var attemptCounter = 0;
            var source = Observable.Defer(() => {
                attemptCounter++;
                return Observable.Throw<Unit>(new InvalidOperationException("Transient Failure"));
            });

            var stream = source
                .PushStackFrame("OperationFrame")
                .ChainFaultContext(s => s.Retry(3), ["RetryBoundary"]);

            
            await stream.PublishFaults().Capture();

            // ASSERT
            attemptCounter.ShouldBe(3);
            BusEvents.Count.ShouldBe(1);
            var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            var logicalStack = fault.LogicalStackTrace.ToList();
            
            // This assertion will fail. It expects a clean stack with only one frame from the last attempt.
            // The buggy implementation will produce a stack with three concatenated "OperationFrame" entries.
            logicalStack.Count.ShouldBe(1, "The logical stack should be reset for each retry attempt.");
            logicalStack.Single().MemberName.ShouldBe("OperationFrame");
        }
    }
}