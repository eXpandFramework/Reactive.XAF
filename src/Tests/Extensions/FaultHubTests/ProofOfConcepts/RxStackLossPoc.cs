using System;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.FaultHub;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Tests.FaultHubTests.ProofOfConcepts{
    [TestFixture]
    public class RxStackLossPoc : FaultHubTestBase {

        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<int> WorkThatFailsOnScheduler() {

            return Observable.Timer(TimeSpan.FromMilliseconds(10))
                .SelectMany(_ => Observable.Throw<int>(new InvalidOperationException("Failure on a background thread.")));
        }

        [Test]
        public void Physical_StackTrace_Is_Null_When_Using_Observable_Throw() {

            Exception caughtException = null;
            var signal = new ManualResetEvent(false);

            var problematicStream = WorkThatFailsOnScheduler();
            
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


            caughtException.ShouldNotBeNull();
            var physicalStackTrace = caughtException.StackTrace;

            physicalStackTrace.ShouldBeNullOrEmpty();
            
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<int> WorkThatFailsOnScheduler_With_Resilience() => WorkThatFailsOnScheduler()
            
            .PushStackFrame();

        [Test]
        public async Task Logical_StackTrace_Is_Preserved_By_PushStackFrame_Across_Schedulers() {
            
            var resilientStream = WorkThatFailsOnScheduler_With_Resilience();

            
            await resilientStream
                .ContinueOnFault()
                .PublishFaults()
                .Capture();

            
            BusEvents.Count.ShouldBe(1);
            var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            var logicalStack = fault.LogicalStackTrace.ToList();

            
            logicalStack.ShouldContain(frame => frame.MemberName == nameof(Logical_StackTrace_Is_Preserved_By_PushStackFrame_Across_Schedulers));

            logicalStack.ShouldContain(frame => frame.MemberName == nameof(WorkThatFailsOnScheduler_With_Resilience));
        }
    }
}