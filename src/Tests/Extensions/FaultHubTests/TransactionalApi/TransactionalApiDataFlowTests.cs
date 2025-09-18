using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Tests.FaultHubTests.TransactionalApi {
    [TestFixture]
    public class TransactionalApiEmissionTests : FaultHubTestBase {
        [Test]
        public async Task Failing_Nested_RunToEnd_Emits_Salvaged_Data_Before_Erroring() {
            // ARRANGE: A nested RunToEnd transaction that succeeds, fails, then succeeds again.
            var nestedTransaction = Observable.Return("start")
                .BeginWorkflow("Nested-Tx")
                .Then(__ => Observable.Return("Salvaged Data A"))
                .Then(__ => Observable.Throw<string>(new InvalidOperationException("Inner Failure")))
                .Then(__ => Observable.Return("Salvaged Data B"))
                .RunToEnd();

            // ACT: We subscribe directly to the nested transaction and capture all its notifications.
            var result = await nestedTransaction.Capture();

            // ASSERT
            result.Error.ShouldNotBeNull("The transaction should have terminated with an error.");
            result.Error.ShouldBeOfType<FaultHubException>();
            
            // This is the critical assertion. It proves whether the OnNext notification
            // containing the salvaged data was emitted before the OnError.
            result.Items.ShouldHaveSingleItem("The salvaged data was not emitted as an OnNext notification before the error.");
            
            var salvagedItems = result.Items.Single();
            salvagedItems.Length.ShouldBe(2);
            salvagedItems.ShouldContain("Salvaged Data A");
            salvagedItems.ShouldContain("Salvaged Data B");
        }
    }
}