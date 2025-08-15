using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using NUnit.Framework;
using Shouldly;

namespace Xpand.Extensions.Tests.FaultHubTests.POC {
    [TestFixture]
    public class FinallyCatchOrderPoc {
        [Test]
        public void Finally_Executes_After_Catch() {
            // ARRANGE
            var executionLog = new List<string>();
            var source = Observable.Throw<int>(new InvalidOperationException("Test Error"));

            // ACT
            var stream = source
                .Finally(() => executionLog.Add("Finally Called"))
                .Catch((Exception _) => {
                    executionLog.Add("Catch Called");
                    return Observable.Empty<int>();
                });

            // Subscribe to trigger the execution.
            stream.Subscribe();

            // ASSERT
            executionLog.Count.ShouldBe(2);
            executionLog[1].ShouldBe("Finally Called");
            executionLog[0].ShouldBe("Catch Called");
        }
    }
}