using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.FaultHub;
using Xpand.Extensions.Reactive.FaultHub.Transaction;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Tests.FaultHubTests.TransactionalApi {
    [TestFixture]
    public class TransactionalApiEmissionTests : FaultHubTestBase {
        [Test]
        public async Task Failing_Nested_RunToEnd_Emits_Salvaged_Data_Before_Erroring() {
            var nestedTransaction = Observable.Return("start")
                .BeginWorkflow("Nested-Tx")
                .Then(_ => Observable.Return("Salvaged Data A"))
                .Then(_ => Observable.Throw<string>(new InvalidOperationException("Inner Failure")))
                .Then(_ => Observable.Return("Salvaged Data B"))
                .RunToEnd();

            var result = await nestedTransaction.Capture();

            result.Error.ShouldNotBeNull("The transaction should have terminated with an error.");
            result.Error.ShouldBeOfType<FaultHubException>();

            result.Items.ShouldHaveSingleItem("The salvaged data was not emitted as an OnNext notification before the error.");
            
            var salvagedItems = result.Items.Single();
            salvagedItems.Length.ShouldBe(2);
            salvagedItems.ShouldContain("Salvaged Data A");
            salvagedItems.ShouldContain("Salvaged Data B");
        }
    }
}