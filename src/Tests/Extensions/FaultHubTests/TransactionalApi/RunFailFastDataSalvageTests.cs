using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.FaultHub;
using Xpand.Extensions.Reactive.FaultHub.Transaction;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Tests.FaultHubTests.TransactionalApi{
    [TestFixture]
    public class RunFailFastDataSalvageTests : FaultHubTestBase {
        private IObservable<string> Step_EmitsPartial_Then_Fails() =>
            Observable.Return("Partial Data")
                .Concat(Observable.Throw<string>(new InvalidOperationException("Step Failed Critically")));

        [Test]
        public async Task Critical_Failure_With_EmitPartialResults_Emits_Data_Then_Aborts() {
            var nextStepExecuted = false;

            var transaction = Observable.Return("start")
                .BeginWorkflow("Critical-EmitPartial-Tx")
                .Then(
                    _ => Step_EmitsPartial_Then_Fails(),
                    dataSalvageStrategy: DataSalvageStrategy.EmitPartialResults
                )
                .Then(_ => {
                    nextStepExecuted = true;
                    return Observable.Return("This step should not run.");
                })
                .RunFailFast();

            var result = await transaction.Capture();

            nextStepExecuted.ShouldBeFalse("Transaction should have aborted on a critical failure.");
            
            result.Items.ShouldHaveSingleItem();
            result.Items.Single().ShouldHaveSingleItem().ShouldBe("Partial Data");

            result.Error.ShouldBeOfType<TransactionAbortedException>();
            result.Error.InnerException.ShouldBeOfType<FaultHubException>()
                .InnerException.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Step Failed Critically");
        }

        [Test]
        public async Task Critical_Failure_With_EmitEmpty_Emits_Nothing_Then_Aborts() {
            var nextStepExecuted = false;

            var transaction = Observable.Return("start")
                .BeginWorkflow("Critical-EmitEmpty-Tx")
                .Then(
                    _ => Step_EmitsPartial_Then_Fails(),
                    dataSalvageStrategy: DataSalvageStrategy.EmitEmpty
                )
                .Then(_ => {
                    nextStepExecuted = true;
                    return Observable.Return("This step should not run.");
                })
                .RunFailFast();

            var result = await transaction.Capture();

            nextStepExecuted.ShouldBeFalse();
            result.Items.ShouldBeEmpty();
            result.Error.ShouldBeOfType<TransactionAbortedException>();
        }

        [Test]
        public async Task Critical_Failure_Inherits_Global_EmitPartialResults_Strategy() {
            var transaction = Observable.Return("start")
                .BeginWorkflow("Critical-Inherit-Tx")
                .Then(
                    _ => Step_EmitsPartial_Then_Fails(),
                    dataSalvageStrategy: DataSalvageStrategy.Inherit 
                )
                .RunFailFast(dataSalvageStrategy: DataSalvageStrategy.EmitPartialResults);

            var result = await transaction.Capture();

            result.Items.ShouldHaveSingleItem();
            result.Items.Single().ShouldHaveSingleItem().ShouldBe("Partial Data");
            result.Error.ShouldBeOfType<TransactionAbortedException>();
        }

        [Test]
        public async Task NonCritical_Failure_With_EmitPartialResults_Continues_And_Passes_Data() {
            var nextStepReceivedCorrectData = false;

            var transaction = Observable.Return("start")
                .BeginWorkflow("NonCritical-EmitPartial-Tx")
                .Then(
                    _ => Step_EmitsPartial_Then_Fails(),
                    dataSalvageStrategy: DataSalvageStrategy.EmitPartialResults
                )
                .Then(partialResults => {
                    partialResults.ShouldHaveSingleItem();
                    partialResults.Single().ShouldBe("Partial Data");
                    nextStepReceivedCorrectData = true;
                    return Observable.Return("Final Step");
                })
                .RunFailFast(isNonCritical: ex => ex is InvalidOperationException);

            var result = await transaction.PublishFaults().Capture();

            nextStepReceivedCorrectData.ShouldBeTrue();
            result.Error.ShouldBeNull("The transaction should have completed with a non-critical aggregate fault, not an error.");
            
            BusEvents.Count.ShouldBe(1);
            var finalFault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            finalFault.Context.Tags.ShouldContain(Transaction.NonCriticalAggregateTag);
            var aggregate = finalFault.InnerException.ShouldBeOfType<AggregateException>();
            aggregate.InnerExceptions.Single().ShouldBeOfType<FaultHubException>()
                .Context.Tags.ShouldContain(Transaction.NonCriticalStepTag);
        }

        [Test]
        public async Task NonCritical_Failure_With_EmitEmpty_Continues_And_Passes_Empty() {
            var nextStepReceivedEmpty = false;

            var transaction = Observable.Return("start")
                .BeginWorkflow("NonCritical-EmitEmpty-Tx")
                .Then(
                    _ => Step_EmitsPartial_Then_Fails(),
                    dataSalvageStrategy: DataSalvageStrategy.EmitEmpty
                )
                .Then(results => {
                    results.ShouldBeEmpty();
                    nextStepReceivedEmpty = true;
                    return Observable.Return("Final Step");
                })
                .RunFailFast(isNonCritical: ex => ex is InvalidOperationException);

            await transaction.PublishFaults().Capture();

            nextStepReceivedEmpty.ShouldBeTrue();
            BusEvents.Count.ShouldBe(1);
            BusEvents.Single().Context.Tags.ShouldContain(Transaction.NonCriticalAggregateTag);
        }
    }
}