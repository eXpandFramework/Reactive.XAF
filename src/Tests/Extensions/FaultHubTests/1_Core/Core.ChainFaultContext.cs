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

namespace Xpand.Extensions.Tests.FaultHubTests._1_Core {
    [TestFixture]
    public class CoreChainFaultContextTests  : FaultHubTestBase {
        

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

            var outerBoundary = innerBoundary
                .TrackSubscriptions(outerCounter)
                .ChainFaultContext(
                    source => source.Retry(3), ["OuterBoundary_Context"]
                );

            
            await outerBoundary.PublishFaults().Capture();

            var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            var logicalStack = fault.LogicalStackTrace.Select(f => f.MemberName).ToArray();
            var allContexts = fault.AllContexts.ToArray();

            outerCounter.Count.ShouldBe(3, "The outer retry policy did not take precedence.");
            innerCounter.Count.ShouldBe(15, "The inner operation was not retried correctly by the outer boundary.");
            
            allContexts.ShouldContain("OuterBoundary_Context");
            allContexts.ShouldContain("InnerBoundary_Context");
            Array.IndexOf(allContexts, "OuterBoundary_Context")
                .ShouldBeLessThan(Array.IndexOf(allContexts, "InnerBoundary_Context"));


            logicalStack.ShouldContain("InnerMost_LogicalFrame");
        }
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> FailingAsyncOperationWithUpstreamStack() {
            return Observable.Timer(TimeSpan.FromMilliseconds(20))
                .SelectMany(_ => Observable.Throw<Unit>(new InvalidOperationException("Async Failure")))
                .PushStackFrame("BusinessLogicFrame");
        }

        [Test]
        public async Task ChainFaultContext_Correctly_Captures_Stack_From_Single_Async_PushStackFrame() {

            
            var stream = FailingAsyncOperationWithUpstreamStack()
                .ChainFaultContext(["Boundary"]);

            
            await stream.PublishFaults().Capture();
            
            BusEvents.Count.ShouldBe(1);
            var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            var logicalStack = fault.LogicalStackTrace.ToList();
            
            logicalStack.ShouldContain(
                frame => frame.MemberName == "BusinessLogicFrame",
                "The logical stack from the async operation should have been preserved."
            );
        }
        
        [Test]
        public async Task ChainFaultContext_Captures_Full_Stack_From_Chained_Async_PushStackFrame_Operators() {

            
            var stream = Observable.Timer(TimeSpan.FromMilliseconds(20))
                .SelectMany(_ => Observable.Throw<Unit>(new InvalidOperationException("Async Failure")))
                .PushStackFrame("InnerFrame") 
                .PushStackFrame("OuterFrame") 
                .ChainFaultContext(["Boundary"]);

            
            await stream.PublishFaults().Capture();
            
            BusEvents.Count.ShouldBe(1);
            var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            var logicalStack = fault.LogicalStackTrace.ToList();

            
            logicalStack.ShouldContain(frame => frame.MemberName == "InnerFrame");
            logicalStack.ShouldContain(frame => frame.MemberName == "OuterFrame");
        }
        
        [Test]
        public async Task ChainFaultContext_With_Retry_Should_Reset_Logical_Stack_On_Each_Attempt() {
            var attemptCounter = 0;
            var source = Observable.Defer(() => {
                attemptCounter++;
                return Observable.Throw<Unit>(new InvalidOperationException("Transient Failure"));
            });

            var stream = source
                .PushStackFrame("OperationFrame")
                .ChainFaultContext(s => s.Retry(3), ["RetryBoundary"]);

            
            await stream.PublishFaults().Capture();
            
            attemptCounter.ShouldBe(3);
            BusEvents.Count.ShouldBe(1);
            var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            var logicalStack = fault.LogicalStackTrace.ToList();


            logicalStack.Count.ShouldBe(1, "The logical stack should be reset for each retry attempt.");
            logicalStack.Single().MemberName.ShouldBe("OperationFrame");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> PoC_Level3_Fails_Async()
            => Observable.Timer(TimeSpan.FromMilliseconds(20))
                .SelectMany(_ => Observable.Throw<Unit>(new InvalidOperationException("Async Failure")))
                .PushStackFrame();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> PoC_Level2_Calls_Level3()
            => PoC_Level3_Fails_Async()
                .PushStackFrame();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> PoC_Level1_Calls_Level2()
            => PoC_Level2_Calls_Level3()
                .PushStackFrame();

        [Test]
        public async Task ChainFaultContext_Should_Preserve_Correct_Order_For_Nested_Async_PushStackFrame() {
    
            var stream = PoC_Level1_Calls_Level2()
                .ChainFaultContext(["Boundary"]);
    
            await stream.PublishFaults().Capture();
    
            BusEvents.Count.ShouldBe(1);
            var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            var logicalStack = fault.LogicalStackTrace.Select(f => f.MemberName).ToArray();

            var level1Index = Array.IndexOf(logicalStack, nameof(PoC_Level1_Calls_Level2));
            var level2Index = Array.IndexOf(logicalStack, nameof(PoC_Level2_Calls_Level3));
            var level3Index = Array.IndexOf(logicalStack, nameof(PoC_Level3_Fails_Async));
    
            level3Index.ShouldNotBe(-1,"Level3 frame not found");
            level2Index.ShouldNotBe(-1,"Level2 frame not found");
            level1Index.ShouldNotBe(-1,"Level1 frame not found");

            level3Index.ShouldBeLessThan(level2Index, "Innermost async frame (Level3) should come before the middle frame (Level2).");
            level2Index.ShouldBeLessThan(level1Index, "Middle frame (Level2) should come before the outer frame (Level1).");
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

            TearDown();
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
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> InnerFailingOperation_WithFrame() =>
            Observable.Throw<Unit>(new InvalidOperationException("Inner Failure"))
                .PushStackFrame("InnerFrame");

        [Test]
        public async Task ChainFaultContext_Captures_Full_Upstream_Stack_Built_During_Subscription() {
            var stream = Observable.Return(Unit.Default)
                .PushStackFrame("OuterFrame")
                .SelectMany(_ => InnerFailingOperation_WithFrame())
                .ChainFaultContext(["Boundary"]);

            await stream.PublishFaults().Capture();

            BusEvents.Count.ShouldBe(1);
            var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            var logicalStack = fault.LogicalStackTrace.ToArray();

            logicalStack.Length.ShouldBe(2, "The logical stack should contain frames from both operators upstream of the boundary.");

            logicalStack.ShouldContain(frame => frame.MemberName == "InnerFrame");
            logicalStack.ShouldContain(frame => frame.MemberName == "OuterFrame");

            var innerFrameIndex = Array.FindIndex(logicalStack, frame => frame.MemberName == "InnerFrame");
            var outerFrameIndex = Array.FindIndex(logicalStack, frame => frame.MemberName == "OuterFrame");

            innerFrameIndex.ShouldBeLessThan(outerFrameIndex, "The InnerFrame, which is subscribed to last, should appear before the OuterFrame in the stack.");
            
        }
        
        

        [Test]
        public async Task PushStackFrame_After_ChainFaultContext_Is_Ignored() {
            var stream = InnerFailingOperation_WithFrame()
                .ChainFaultContext(["Boundary"])
                .PushStackFrame("OuterFrame_ToBeIgnored");

            await stream.PublishFaults().Capture();

            BusEvents.Count.ShouldBe(1);
            var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            var logicalStack = fault.LogicalStackTrace.ToArray();

            logicalStack.Length.ShouldBe(1, "The logical stack should only contain the frame captured by ChainFaultContext.");
            logicalStack.ShouldContain(frame => frame.MemberName == "InnerFrame");
            logicalStack.ShouldNotContain(frame => frame.MemberName == "OuterFrame_ToBeIgnored",
                "The frame from PushStackFrame after the boundary should have been ignored.");
        }
    }
    }
