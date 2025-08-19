using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading;
using NUnit.Framework;
using Shouldly;

namespace Xpand.Extensions.Tests.FaultHubTests.POC.RX {
    [TestFixture]
    public class AsyncFinallyCatchOrderPoc {
        private static readonly AsyncLocal<string> TestContext = new();
        
        [Test]
        public void AsyncLocal_Context_Is_Preserved_In_Catch_and_Finally_When_Error_Is_From_Different_Scheduler() {
            
            var executionLog = new List<string>();
            string contextInFinally = "NOT_SET";
            string contextInCatch = "NOT_SET";
            var signal = new CountdownEvent(2);

            
            TestContext.Value = "CONTEXT_SET_ON_MAIN_THREAD";

            var stream = Observable.Timer(TimeSpan.FromMilliseconds(20))
                .SelectMany(_ => Observable.Throw<long>(new InvalidOperationException("Error from background thread")))
                .Finally(() => {
                    executionLog.Add("Finally Called");
                    contextInFinally = TestContext.Value;
                    signal.Signal();
                })
                .Catch((Exception _) => {
                    executionLog.Add("Catch Called");
                    contextInCatch = TestContext.Value;
                    signal.Signal();
                    return Observable.Empty<long>();
                });

            using (stream.Subscribe()) {
    
                signal.Wait(TimeSpan.FromSeconds(5)).ShouldBeTrue("The test timed out waiting for both Catch and Finally blocks to execute.");
            }

    
            executionLog.Count.ShouldBe(2);
            executionLog.ShouldContain("Catch Called");
            executionLog.ShouldContain("Finally Called");

    
            contextInCatch.ShouldBe("CONTEXT_SET_ON_MAIN_THREAD", "The context was lost in the Catch block.");
            contextInFinally.ShouldBe("CONTEXT_SET_ON_MAIN_THREAD", "The context was lost in the Finally block.");
        }
        
    }

}
