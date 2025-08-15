using System;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Tests.FaultHubTests.POC{
    [TestFixture]
    public class RxStackLossPoc : FaultHubTestBase {
        // A helper method that simulates a piece of work that fails on a background thread.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<int> WorkThatFailsOnScheduler() {
            // Using Timer ensures the action runs on a separate scheduler thread.
            return Observable.Timer(TimeSpan.FromMilliseconds(10))
                .SelectMany(_ => Observable.Throw<int>(new InvalidOperationException("Failure on a background thread.")));
        }

        [Test]
        public void Physical_StackTrace_Is_Null_When_Using_Observable_Throw() {
            // ARRANGE: This test demonstrates the classic Rx problem.
            Exception caughtException = null;
            var signal = new ManualResetEvent(false);

            var problematicStream = WorkThatFailsOnScheduler();

            // ACT: We subscribe directly without any FaultHub operators.
            using (problematicStream.Subscribe(
                       onNext: _ => { },
                       onError: ex => {
                           caughtException = ex;
                           signal.Set();
                       },
                       onCompleted: () => signal.Set()
                   )) {
                signal.WaitOne(TimeSpan.FromSeconds(5)).ShouldBeTrue("Test timed out.");
            }

            // ASSERT: The physical stack trace is not useful.
            caughtException.ShouldNotBeNull();
            var physicalStackTrace = caughtException.StackTrace;

            physicalStackTrace.ShouldBeNullOrEmpty();
            
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<int> WorkThatFailsOnScheduler_With_Resilience() {
            return WorkThatFailsOnScheduler()
                // The SOLUTION: PushStackFrame captures the caller's context before the work
                // is passed to the scheduler.
                .PushStackFrame();
        }

        [Test]
        public async Task Logical_StackTrace_Is_Preserved_By_PushStackFrame_Across_Schedulers() {
            // ARRANGE: This test demonstrates the FaultHub solution.
            var resilientStream = WorkThatFailsOnScheduler_With_Resilience();

            // ACT: We use the standard FaultHub pattern to suppress and publish the error.
            await resilientStream
                .ContinueOnFault()
                .PublishFaults()
                .Capture();

            // ASSERT: The captured FaultHubException contains a complete and useful LOGICAL stack trace.
            BusEvents.Count.ShouldBe(1);
            var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            var logicalStack = fault.LogicalStackTrace.ToList();

            // The logical stack contains the full story, from the test method (the consumer)...
            logicalStack.ShouldContain(frame => frame.MemberName == nameof(Logical_StackTrace_Is_Preserved_By_PushStackFrame_Across_Schedulers));
            // ...down to the helper method that applied the resilience.
            logicalStack.ShouldContain(frame => frame.MemberName == nameof(WorkThatFailsOnScheduler_With_Resilience));
        }
    }
}