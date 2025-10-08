using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.Relay;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Tests.FaultHubTests.Core {
    [TestFixture]
    public class PushStackFrameIsolationTests : FaultHubTestBase {
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private IObservable<long> InnerFailingStreamWithFrame() 
            => Observable.Throw<long>(new InvalidOperationException("Stream C Error"))
                .PushStackFrame();
        [Test]
        public async Task Should_Capture_Nested_Stack_For_Failing_Inner_Stream() {
            var stream = Observable.Return(0L)
                .PushStackFrame()
                .SelectMany(_ => InnerFailingStreamWithFrame());

            await stream.ChainFaultContext().PublishFaults().Capture();

            BusEvents.Count.ShouldBe(1);
            var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            var stack = fault.LogicalStackTrace.ToArray();

            stack.Length.ShouldBe(2);
            stack[0].MemberName.ShouldBe(nameof(InnerFailingStreamWithFrame));
            stack[1].MemberName.ShouldBe(nameof(Should_Capture_Nested_Stack_For_Failing_Inner_Stream));
        }

        [Test]
        public async Task Should_Maintain_Stack_Isolation_For_Concurrent_Streams() {
            var streamA = Observable.Return(0L)
                .PushStackFrame("FrameA")
                .SelectMany(_ => Observable.Return(0L).Delay(TimeSpan.FromSeconds(1)).PushStackFrame("InnerFrameA"));

            var streamB = Observable.Return(0L)
                .PushStackFrame("FrameB")
                .SelectMany(_ => Observable.Throw<long>(new InvalidOperationException("Stream D Error"))
                    .PushStackFrame("InnerFrameB"));

            await streamA.PublishFaults().Merge(streamB.ChainFaultContext().PublishFaults()).Capture();
            
            BusEvents.Count.ShouldBe(1);
            var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            var stack = fault.LogicalStackTrace.Select(f => f.MemberName).ToArray();

            stack.Length.ShouldBe(3);
            stack.ShouldContain("FrameB");
            stack.ShouldContain("InnerFrameB");
            stack.ShouldNotContain("FrameA");
            stack.ShouldNotContain("InnerFrameA");
        }

        [Test]
        public async Task Should_Preserve_Stack_Across_Scheduler_Boundaries() {
            var stream = Observable.Return(0L)
                .SubscribeOn(TaskPoolScheduler.Default)
                .PushStackFrame("Frame1")
                .ObserveOn(TaskPoolScheduler.Default)
                .PushStackFrame("Frame2")
                .SelectMany(_ => Observable.Throw<long>(new InvalidOperationException("Scheduler Error")))
                .PushStackFrame("Frame3");

            await stream.ChainFaultContext().PublishFaults().Capture();

            BusEvents.Count.ShouldBe(1);
            var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            var stack = fault.LogicalStackTrace.Select(f => f.MemberName).ToArray();

            stack.Length.ShouldBe(4);
            stack[0].ShouldBe(nameof(Should_Preserve_Stack_Across_Scheduler_Boundaries));
            stack[1].ShouldBe("Frame3");
            stack[2].ShouldBe("Frame2");
            stack[3].ShouldBe("Frame1");
        }

        [Test]
        public async Task Should_Preserve_Stack_For_Failing_Inner_Stream_Across_ObserveOn() {
            var stream = Observable.Return(0L)
                .PushStackFrame("Frame1")
                .ObserveOn(Scheduler.Default)
                .SelectMany(_ => Observable.Throw<long>(new InvalidOperationException("Inner Stream Error"))
                        .PushStackFrame("Frame2"));

            await stream.ChainFaultContext().PublishFaults().Capture();

            BusEvents.Count.ShouldBe(1);
            var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            var stack = fault.LogicalStackTrace.Select(f => f.MemberName).ToArray();
            
            stack.Length.ShouldBe(3);
            stack[0].ShouldBe(nameof(Should_Preserve_Stack_For_Failing_Inner_Stream_Across_ObserveOn));
            stack[1].ShouldBe("Frame2");
            stack[2].ShouldBe("Frame1");
        }

        [Test]
        public async Task Should_Discard_Failed_Process_Stack_And_Preserve_Recovery_Stack_With_Catch() {
            var stream = Observable.Return(0L)
                .PushStackFrame("FrameA")
                .SelectMany(_ => Observable.Throw<long>(new InvalidOperationException("Inner Failing Process")).PushStackFrame("FrameB"))
                .ChainFaultContext()
                .Catch((Exception _) => Observable.Throw<long>(new InvalidOperationException("Recovery Process Error")).PushStackFrame("FrameC"));

            await stream.ChainFaultContext().PublishFaults().Capture();

            BusEvents.Count.ShouldBe(1);
            var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            fault.InnerException.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Recovery Process Error");
            var stack = fault.LogicalStackTrace.Select(f => f.MemberName).ToArray();

            stack.ShouldNotContain("FrameA");
            stack.ShouldNotContain("FrameB");
            stack.ShouldContain("FrameC");
        }

        [Test]
        public async Task Should_Clear_Stack_Between_Retry_Attempts() {
            var attemptCount = 0;
            var stream = Observable.Defer(() => {
                    attemptCount++;
                    return Observable.Return(0L)
                        .SelectMany(_ => Observable.Throw<long>(new InvalidOperationException($"Attempt {attemptCount} failed")))
                        .PushStackFrame("AttemptFrame");
                })
                .Retry(3);

            await stream.ChainFaultContext().PublishFaults().Capture();

            attemptCount.ShouldBe(3);
            BusEvents.Count.ShouldBe(1);
            var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            var stack = fault.LogicalStackTrace.Select(f => f.MemberName).ToArray();

            stack.Length.ShouldBe(2, "The stack should only contain frames from the final attempt.");
            stack.ShouldContain("AttemptFrame");
        }

        [Test]
        public async Task Should_Maintain_Stack_Isolation_For_Multiple_Subscribers_To_Hot_Observable() {
            
            var source = new ReplaySubject<long>(1);
            var hotSource = source.Publish();

            var streamA = hotSource
                .SelectMany(_ => Observable.Throw<long>(new InvalidOperationException("Error A")))
                .PushStackFrame("FrameA");

            var streamB = hotSource
                .SelectMany(_ => Observable.Throw<long>(new InvalidOperationException("Error B")))
                .PushStackFrame("FrameB");

            var testStream = Observable.Create<Unit>(observer => {
                var streamAWithFaults = streamA.ChainFaultContext().PublishFaults().ToUnit();
                var streamBWithFaults = streamB.ChainFaultContext().PublishFaults().ToUnit();

                var mergedSubscription = streamAWithFaults.Merge(streamBWithFaults).Subscribe(
                    _ => { },
                    observer.OnError,
                    observer.OnCompleted
                );

                var connection = hotSource.Connect();
                source.OnNext(1);
                source.OnCompleted();

                return new CompositeDisposable(mergedSubscription, connection);
            });

            await testStream.Capture();

            BusEvents.Count.ShouldBe(2);

            var faultA = BusEvents.Single(f => f.InnerException?.Message == "Error A");
            var stackA = faultA.LogicalStackTrace.Select(f => f.MemberName).ToArray();
            stackA.ShouldContain("FrameA");
            stackA.ShouldNotContain("FrameB");

            var faultB = BusEvents.Single(f => f.InnerException?.Message == "Error B");
            var stackB = faultB.LogicalStackTrace.Select(f => f.MemberName).ToArray();
            stackB.ShouldContain("FrameB");
            stackB.ShouldNotContain("FrameA");
        }

        [Test]
        public async Task Should_Preserve_Stack_Across_Time_Based_Delay_Operator_With_Real_Scheduler() {
            var stream = Observable.Return(0L)
                .Delay(TimeSpan.FromMilliseconds(50), Scheduler.Default)
                .PushStackFrame("FrameA")
                .SelectMany(_ => Observable.Throw<long>(new InvalidOperationException("Error after delay")))
                .PushStackFrame("FrameB");

            await stream.ChainFaultContext().PublishFaults().Capture();

            BusEvents.Count.ShouldBe(1);
            var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            var stack = fault.LogicalStackTrace.Select(f => f.MemberName).ToArray();

            stack.Length.ShouldBe(3);
            stack[0].ShouldBe(nameof(Should_Preserve_Stack_Across_Time_Based_Delay_Operator_With_Real_Scheduler));
            stack[1].ShouldBe("FrameB");
            stack[2].ShouldBe("FrameA");
        }

        [Test]
        public async Task Should_Preserve_Stack_In_Backpressure_Scenario_With_Throttle() {
            var stream = Observable.Interval(TimeSpan.FromMilliseconds(1))
                .Take(5)
                .Throttle(TimeSpan.FromMilliseconds(50))
                .PushStackFrame("FrameA")
                .SelectMany(_ => Observable.Throw<long>(new InvalidOperationException("Error after throttle")))
                .PushStackFrame("FrameB");

            await stream.ChainFaultContext().PublishFaults().Capture();

            BusEvents.Count.ShouldBe(1);
            var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            var stack = fault.LogicalStackTrace.Select(f => f.MemberName).ToArray();

            stack.Length.ShouldBe(3);
            stack[0].ShouldBe(nameof(Should_Preserve_Stack_In_Backpressure_Scenario_With_Throttle));
            stack[1].ShouldBe("FrameB");
            stack[2].ShouldBe("FrameA");
        }

        [Test]
        public async Task Should_Clear_Stack_After_Explicit_Disposal() {
            var streamA = Observable.Never<long>().PushStackFrame("FrameA");
            using (streamA.Subscribe()) { }

            var streamB = Observable.Return(0L)
                .SelectMany(_ => Observable.Throw<long>(new InvalidOperationException("Error B")))
                .PushStackFrame("FrameB");

            await streamB.ChainFaultContext().PublishFaults().Capture();

            BusEvents.Count.ShouldBe(1);
            var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            var stack = fault.LogicalStackTrace.Select(f => f.MemberName).ToArray();

            stack.Length.ShouldBe(2);
            stack.ShouldContain("FrameB");
            stack.ShouldNotContain("FrameA", "Context from disposed streamA leaked into streamB's fault.");
        }
    }
}