using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using NUnit.Framework;
using Shouldly;

namespace Xpand.Extensions.Tests.Reactive.ProofOfConcepts {
    [TestFixture]
    public class FinallyCatchOrderPoc {
        [Test]
        public void Finally_Executes_After_Catch() {
            
            var executionLog = new List<string>();
            var source = Observable.Throw<int>(new InvalidOperationException("Test Error"));

            
            var stream = source
                .Finally(() => executionLog.Add("Finally Called"))
                .Catch((Exception _) => {
                    executionLog.Add("Catch Called");
                    return Observable.Empty<int>();
                });

          
            stream.Subscribe();

          
            executionLog.Count.ShouldBe(2);
            executionLog[1].ShouldBe("Finally Called");
            executionLog[0].ShouldBe("Catch Called");
        }
    }
}