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
    public class DataSalvageStrategyTests : FaultHubTestBase {
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
                    dataSalvageStrategy: DataSalvageStrategy.EmitEmpty
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
        public async Task RunFailFast_Ignores_DataSalvageStrategy_And_Aborts() {
            var nextStepExecuted = false;

            var transaction = Observable.Return("start")
                .BeginWorkflow("FailFast-Ignore-Tx")
                .Then(
                    _ => Step_EmitsPartial_Then_Fails(),
                    dataSalvageStrategy: DataSalvageStrategy.EmitPartialResults)
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
        
        [Test]
        public async Task RunToEnd_With_Global_EmitEmpty_Strategy_Applies_To_All_Steps() {
            var nextStepReceivedEmpty = false;

            var transaction = Observable.Return("start")
                .BeginWorkflow("GlobalEmitEmpty-Tx")
                .Then(_ => Step_EmitsPartial_Then_Fails())
                .Then(results => {
                    results.ShouldBeEmpty();
                    nextStepReceivedEmpty = true;
                    return Observable.Return("Final Step");
                })
                .RunToEnd(dataSalvageStrategy: DataSalvageStrategy.EmitEmpty);

            await transaction.PublishFaults().Capture();

            nextStepReceivedEmpty.ShouldBeTrue("The step did not inherit the global EmitEmpty strategy.");
            BusEvents.Count.ShouldBe(1);
        }

        [Test]
        public async Task Then_Local_Strategy_Overrides_RunToEnd_Global_Strategy() {
            var step2ReceivedEmpty = false;
            var step3ReceivedPartials = false;

            var transaction = Observable.Return("start")
                .BeginWorkflow("Override-Tx")
                .Then(
                    _ => Step_EmitsPartial_Then_Fails(),
                    stepName: "Step1_Inherits")
                .Then(
                    results => {
                        results.ShouldBeEmpty();
                        step2ReceivedEmpty = true;
                        return Step_EmitsPartial_Then_Fails();
                    },
                    stepName: "Step2_Overrides", dataSalvageStrategy: DataSalvageStrategy.EmitPartialResults
                )
                .Then(
                    results => {
                        results.ShouldHaveSingleItem();
                        results.Single().ShouldBe("Partial Data");
                        step3ReceivedPartials = true;
                        return Observable.Return("Final Step");
                    },
                    stepName: "Step3_Receives"
                )
                .RunToEnd(dataSalvageStrategy: DataSalvageStrategy.EmitEmpty);

            await transaction.PublishFaults().Capture();

            step2ReceivedEmpty.ShouldBeTrue("Step 2 should have received an empty collection from Step 1.");
            step3ReceivedPartials.ShouldBeTrue("Step 3 should have received partial results from Step 2 due to the override.");
            BusEvents.Count.ShouldBe(1);
        }
    }
}