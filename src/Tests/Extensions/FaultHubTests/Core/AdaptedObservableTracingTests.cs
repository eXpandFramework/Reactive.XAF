using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.Relay;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Tests.FaultHubTests.Core
{
    [TestFixture]
    public class AdaptedObservableTracingTests : FaultHubTestBase
    {
        [Test]
        public async Task Chain_WhenMergedStreamErrors_CapturesCorrectPathExcludingNonFailingStream()
        {
            var streamA = new Subject<int>();
            var streamB = new Subject<int>();
            var testException = new Exception("Test Failure");

            var query = streamA.PushStackFrame(memberName: "StreamA")
                .Merge(streamB.PushStackFrame(memberName: "StreamB"))
                .PushStackFrame(memberName: "Projection")
                .ChainFaultContext()
                .PublishFaults();

            using (query.Subscribe())
            {
                streamA.OnNext(1);
                streamB.OnNext(2);
                streamA.OnNext(3);
                
                var busTask = FaultHub.Bus.Take(1).ToTask();
                streamB.OnError(testException);
                streamA.OnCompleted();
                
                await busTask;
            }

            BusEvents.Count.ShouldBe(1);
            var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            fault.InnerException.ShouldBe(testException);

            var stack = fault.LogicalStackTrace.Select(f => f.MemberName).ToArray();
            
            // The path is built from the error source outwards: [Source, Operator1, Operator2, ..., Boundary]
            stack.ShouldContain("StreamB");
            stack.ShouldContain("Projection");
            stack.ShouldNotContain("StreamA");
        }
        
        [Test]
        public async Task Chain_WhenInnerStreamFromSelectManyErrors_CapturesCorrectHierarchicalPath()
        {
            var source = new Subject<int>();
            var testException = new Exception("Inner Failure");

            var query = source
                .PushStackFrame(memberName: "OuterStream")
                .SelectMany(i => {
                    if (i == 1) return Observable.Return($"Success-{i}");
                    return Observable.Throw<string>(testException).PushStackFrame(memberName: "InnerStream-Failure");
                })
                .ChainFaultContext()
                .PublishFaults();

            using (query.Subscribe())
            {
                var busTask = FaultHub.Bus.Take(1).ToTask();
                source.OnNext(1);
                source.OnNext(2);
                await busTask;
            }

            BusEvents.Count.ShouldBe(1);
            var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            fault.InnerException.ShouldBe(testException);

            var stack = fault.LogicalStackTrace.Select(f => f.MemberName).ToArray();
            
            // The path correctly shows the error originating from the inner stream and propagating to the outer.
            var innerFrameIndex = Array.IndexOf(stack, "InnerStream-Failure");
            var outerFrameIndex = Array.IndexOf(stack, "OuterStream");

            innerFrameIndex.ShouldNotBe(-1);
            outerFrameIndex.ShouldNotBe(-1);
            innerFrameIndex.ShouldBeLessThan(outerFrameIndex);
        }
        

        [Test]
        public async Task Chain_WhenObserveOnIsUsed_CorrectlyFlowsContextAcrossThreads()
        {
            var source = new Subject<int>();
            var testException = new Exception("Concurrency Failure");

            var query = source
                .PushStackFrame(memberName: "FrameA")
                .ObserveOn(ThreadPoolScheduler.Instance)
                .PushStackFrame(memberName: "FrameB")
                .ChainFaultContext()
                .PublishFaults();

            using (query.Subscribe())
            {
                var busTask = FaultHub.Bus.Take(1).ToTask();
                source.OnError(testException);
                await busTask;
            }

            BusEvents.Count.ShouldBe(1);
            var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            fault.InnerException.ShouldBe(testException);
            
            var stack = fault.LogicalStackTrace.Select(f => f.MemberName).ToArray();
            var frameAIndex = Array.IndexOf(stack, "FrameA");
            var frameBIndex = Array.IndexOf(stack, "FrameB");

            frameAIndex.ShouldNotBe(-1);
            frameBIndex.ShouldNotBe(-1);
            frameAIndex.ShouldBeLessThan(frameBIndex);
        }

        [Test]
        public async Task Chain_WhenErrorOccursInUsing_CorrectlyCapturesPathAndDisposesResource()
        {
            var source = new Subject<int>();
            var testException = new Exception("Using Failure");
            TestResource resource = null;

            var query = Observable.Using(
                    () => {
                        resource = new TestResource();
                        return resource;
                    },
                    _ => source.PushStackFrame(memberName: "InsideUsing")
                )
                .PushStackFrame(memberName: "OutsideUsing")
                .ChainFaultContext()
                .PublishFaults();

            using (query.Subscribe())
            {
                var busTask = FaultHub.Bus.Take(1).ToTask();
                source.OnError(testException);
                await busTask;
            }

            resource.ShouldNotBeNull();
            resource.IsDisposed.ShouldBeTrue();
            
            BusEvents.Count.ShouldBe(1);
            var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            fault.InnerException.ShouldBe(testException);
            
            var stack = fault.LogicalStackTrace.Select(f => f.MemberName).ToArray();
            stack.ShouldContain("InsideUsing");
            stack.ShouldContain("OutsideUsing");
        }

        [Test]
        public async Task Chain_WithNestedSelectMany_CapturesFullHierarchicalPath()
        {
            var source = new Subject<string>();
            var testException = new Exception("Deep Failure");

            var query = source
                .PushStackFrame(memberName: "Level1")
                .SelectMany(outerId => 
                    Observable.Return($"{outerId}-subtask")
                        .PushStackFrame(memberName: "Level2")
                        .SelectMany(_ => {
                            if (outerId == "request-1") {
                                return Observable.Return("Success");
                            }
                            return Observable.Throw<string>(testException)
                                .PushStackFrame(memberName: "Level3-Failure");
                        })
                )
                .ChainFaultContext()
                .PublishFaults();

            using (query.Subscribe())
            {
                var busTask = FaultHub.Bus.Take(1).ToTask();
                source.OnNext("request-1");
                source.OnNext("request-2");
                await busTask;
            }

            BusEvents.Count.ShouldBe(1);
            var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            fault.InnerException.ShouldBe(testException);
        
            var stack = fault.LogicalStackTrace.Select(f => f.MemberName).ToArray();
            var l1 = Array.IndexOf(stack, "Level1");
            var l2 = Array.IndexOf(stack, "Level2");
            var l3 = Array.IndexOf(stack, "Level3-Failure");

            l1.ShouldNotBe(-1);
            l2.ShouldNotBe(-1);
            l3.ShouldNotBe(-1);

            l3.ShouldBeLessThan(l2);
            l2.ShouldBeLessThan(l1);
        }
        
        [Test]
        public async Task Chain_WhenTimeoutOperatorTriggers_CapturesCorrectPath()
        {
            var source = new Subject<int>();

            var query = source
                .PushStackFrame(memberName: "BeforeTimeout")
                .Timeout(TimeSpan.FromMilliseconds(50))
                .PushStackFrame(memberName: "AfterTimeout")
                .ChainFaultContext()
                .PublishFaults();

            using (query.Subscribe(_ => {}, _ => {}))
            {
                var busTask = FaultHub.Bus.Take(1).ToTask();
                source.OnNext(1); 
                Thread.Sleep(100);
                await busTask;
            }

            BusEvents.Count.ShouldBe(1);
            var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            fault.InnerException.ShouldBeOfType<TimeoutException>();
        
            var stack = fault.LogicalStackTrace.Select(f => f.MemberName).ToArray();
            stack.ShouldContain("AfterTimeout");
            stack.ShouldContain("BeforeTimeout", "Context from before the Timeout operator should now be preserved.");
            Array.IndexOf(stack, "AfterTimeout").ShouldBeLessThan(Array.IndexOf(stack, "BeforeTimeout"));
        }
        
        [Test]
        public async Task Chain_WhenErrorOriginatesInNonInstrumentedStream_CapturesBoundaryFrame()
        {
            var testException = new Exception("Non-Instrumented Failure");

            var query = Observable.Throw<int>(testException)
                .ChainFaultContext()
                .PublishFaults();

            await query.Capture();

            BusEvents.Count.ShouldBe(1);
            var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            fault.InnerException.ShouldBe(testException);
            
            var stack = fault.LogicalStackTrace.ToList();
            stack.ShouldHaveSingleItem();
            stack.Single().MemberName.ShouldBe(nameof(Chain_WhenErrorOriginatesInNonInstrumentedStream_CapturesBoundaryFrame));
        }
        
        [Test]
        public async Task Chain_WhenSourceCompletesSuccessfully_PropagatesOnCompleted()
        {
            var wasCompleted = false;

            var query = Observable.Return(1)
                .PushStackFrame(memberName: "SourceStream")
                .ChainFaultContext()
                .PublishFaults();

            await query.Do(_ => { }, () => wasCompleted = true);

            wasCompleted.ShouldBeTrue("OnCompleted was not propagated.");
            BusEvents.ShouldBeEmpty("Error handler was called on a successful stream.");
        }
        
        [Test]
        public async Task Chain_WhenSubscribedSequentially_DoesNotPolluteContext()
        {
            var query1 = Observable.Throw<int>(new Exception("Failure 1"))
                .PushStackFrame(memberName: "Stream1")
                .ChainFaultContext()
                .PublishFaults();

            var query2 = Observable.Throw<int>(new Exception("Failure 2"))
                .PushStackFrame(memberName: "Stream2")
                .ChainFaultContext()
                .PublishFaults();

            await query1.Capture();
            
            BusEvents.Count.ShouldBe(1);
            var fault1 = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            var stack1 = fault1.LogicalStackTrace.Select(f => f.MemberName).ToArray();
            stack1.ShouldContain("Stream1");
            stack1.ShouldNotContain("Stream2");

            BusEvents.Clear();

            await query2.Capture();
            
            BusEvents.Count.ShouldBe(1);
            var fault2 = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            var stack2 = fault2.LogicalStackTrace.Select(f => f.MemberName).ToArray();
            stack2.ShouldContain("Stream2", "Context from the first stream leaked into the second.");
            stack2.ShouldNotContain("Stream1");
        }
    }
}