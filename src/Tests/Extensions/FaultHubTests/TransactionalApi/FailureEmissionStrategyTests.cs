using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Tests.FaultHubTests.TransactionalApi {
    [TestFixture]
    public class FailureEmissionStrategyTests : FaultHubTestBase {
        private IObservable<string> Step_EmitsPartial_Then_Fails() =>
            Observable.Return("Partial Data")
                .Concat(Observable.Throw<string>(new InvalidOperationException("Step Failed")));

        [Test]
        public async Task RunToEnd_With_Default_Strategy_Emits_Partial_Results() {
            var nextStepReceivedCorrectData = false;

            var transaction = Observable.Return("start")
                .BeginWorkflow("PartialResults-Tx")
                .Then(_ => Step_EmitsPartial_Then_Fails())
                .Then(partialResults => {
                    partialResults.ShouldHaveSingleItem();
                    partialResults.Single().ShouldBe("Partial Data");
                    nextStepReceivedCorrectData = true;
                    return Observable.Return("Final Step");
                })
                .RunToEnd();

            await transaction.PublishFaults().Capture();

            nextStepReceivedCorrectData.ShouldBeTrue("The next step did not receive the partial results as expected.");
            BusEvents.Count.ShouldBe(1);
            var finalFault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            var aggregate = finalFault.InnerException.ShouldBeOfType<AggregateException>();
            aggregate.InnerExceptions.Single().ShouldBeOfType<FaultHubException>()
                .InnerException.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Step Failed");
        }

        [Test]
        public async Task RunToEnd_With_EmitEmpty_Strategy_Emits_No_Results() {
            var nextStepReceivedEmpty = false;

            var transaction = Observable.Return("start")
                .BeginWorkflow("EmitEmpty-Tx")
                .Then(
                    _ => Step_EmitsPartial_Then_Fails(),
                    emissionStrategy: FailureEmissionStrategy.EmitEmpty
                )
                .Then(results => {
                    results.ShouldBeEmpty();
                    nextStepReceivedEmpty = true;
                    return Observable.Return("Final Step");
                })
                .RunToEnd();

            await transaction.PublishFaults().Capture();

            nextStepReceivedEmpty.ShouldBeTrue("The next step did not receive an empty collection as expected.");
            BusEvents.Count.ShouldBe(1);
            var finalFault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            var aggregate = finalFault.InnerException.ShouldBeOfType<AggregateException>();
            aggregate.InnerExceptions.Single().ShouldBeOfType<FaultHubException>()
                .InnerException.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Step Failed");
        }

        [Test]
        public async Task RunFailFast_Ignores_EmissionStrategy_And_Aborts() {
            var nextStepExecuted = false;

            var transaction = Observable.Return("start")
                .BeginWorkflow("FailFast-Ignore-Tx")
                .Then(
                    _ => Step_EmitsPartial_Then_Fails(),
                    emissionStrategy: FailureEmissionStrategy.EmitPartialResults // This should be ignored
                )
                .Then(_ => {
                    nextStepExecuted = true;
                    return Observable.Return("This step should not run.");
                })
                .RunFailFast();

            await transaction.PublishFaults().Capture();

            nextStepExecuted.ShouldBeFalse("RunFailFast should have aborted the transaction before the next step.");
            BusEvents.Count.ShouldBe(1);
            var abortedException = BusEvents.Single().ShouldBeOfType<TransactionAbortedException>();
            abortedException.InnerException.ShouldBeOfType<FaultHubException>()
                .InnerException.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Step Failed");
        }
    }
}