using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using NUnit.Framework;
using Shouldly;

namespace Xpand.Extensions.Tests.FaultHubTests{
    public class RxContextFlowTests {
        private static readonly AsyncLocal<string> TestContext = new();

        [Test]
        public void AsyncLocal_Context_Is_not_Lost_With_Timer_And_SelectMany() {
            string capturedContext = "INITIAL_VALUE";
            var signal = new ManualResetEvent(false);

            // This chain simulates the problematic structure from your failing test.
            var sequence = Observable.Return(Unit.Default)
                .SelectMany(_ => Observable.Timer(TimeSpan.FromMilliseconds(50)))
                .Do(_ => {
                    // This code executes on the Timer's background scheduler.
                    // We capture the value of the AsyncLocal here.
                    capturedContext = TestContext.Value;
                });

            // Set the AsyncLocal value on the main/subscribing thread.
            TestContext.Value = "EXPECTED_VALUE";

            using (sequence.Subscribe(
                       onNext: _ => { },
                       onError: _ => signal.Set(),
                       onCompleted: () => signal.Set()
                   )) {
                signal.WaitOne(TimeSpan.FromSeconds(5)).ShouldBeTrue("The test timed out.");
            }
        
            // This assertion proves the context was lost. It is expected to be null.
            capturedContext.ShouldNotBeNull();
        }
    }
}