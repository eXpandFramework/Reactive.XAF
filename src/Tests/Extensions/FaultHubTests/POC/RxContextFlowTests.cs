using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using NUnit.Framework;
using Shouldly;

namespace Xpand.Extensions.Tests.FaultHubTests.POC{
    public class RxContextFlowTests {
        private static readonly AsyncLocal<string> TestContext = new();

        [Test]
        public void AsyncLocal_Context_Is_not_Lost_With_Timer_And_SelectMany() {
            string capturedContext = "INITIAL_VALUE";
            var signal = new ManualResetEvent(false);
            
            var sequence = Observable.Return(Unit.Default)
                .SelectMany(_ => Observable.Timer(TimeSpan.FromMilliseconds(50)))
                .Do(_ => {
                    capturedContext = TestContext.Value;
                });
            
            TestContext.Value = "EXPECTED_VALUE";

            using (sequence.Subscribe(
                       onNext: _ => { },
                       onError: _ => signal.Set(),
                       onCompleted: () => signal.Set()
                   )) {
                signal.WaitOne(TimeSpan.FromSeconds(5)).ShouldBeTrue("The test timed out.");
            }
            
            capturedContext.ShouldNotBeNull();
        }
    }
}