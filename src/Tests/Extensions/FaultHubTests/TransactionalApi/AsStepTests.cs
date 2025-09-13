using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Tests.FaultHubTests.TransactionalApi;
[TestFixture]
public class TransactionalApiIntegrationTests : FaultHubTestBase {
    public class ExternalService {
        public IObservable<Unit> ExecuteWorkflow(
            Func<string, IObservable<string>> step1,
            Func<string, IObservable<Unit>> step2) {
            return Observable.Defer(() => {
                Console.WriteLine("[CORRELATION_TRACE] ExecuteWorkflow subscribed to. Chaining Step 1.");
                var step1Result = Observable.Return("start").SelectMany(step1);
                Console.WriteLine("[CORRELATION_TRACE] Chaining Step 2.");
                var step2Result = step1Result.SelectMany(step2);
                Console.WriteLine("[CORRELATION_TRACE] Returning final observable.");
                return step2Result;
            });
        }
        public IObservable<Unit> WhenVisitPage(
            Func<string, IObservable<string>> parsePageSelector,
            Func<string, IObservable<Unit>> browserSelector) {
            return Observable.Defer(() => 
                Observable.Return("mockDriver")
                    .SelectMany(parsePageSelector)
                    .SelectMany(browserSelector)
            );
        }
    }

    [SuppressMessage("ReSharper", "UnusedParameter.Local")]
    private IObservable<string> MockParsePageThatFails(string driver) =>
        Observable.Throw<string>(new InvalidOperationException("Parsing Failed"));

    [SuppressMessage("ReSharper", "UnusedParameter.Local")]
    private IObservable<Unit> MockBrowserSelector(string parseResult) => 
        Observable.Return(Unit.Default);
    
    [Test]
    public async Task Fluent_API_Correlates_Steps_From_Inner_Selectors() {
        var service = new ExternalService();

        var transaction = service.WhenVisitPage(
                driver => MockParsePageThatFails(driver)
                    .AsStep(),
                parseResult => MockBrowserSelector(parseResult)
                    .AsStep()
            )
            .BeginWorkflow()
            .RunFailFast();

        await transaction.PublishFaults().Capture();

        BusEvents.Count.ShouldBe(1);
        var finalReport = BusEvents.Single().ShouldBeOfType<TransactionAbortedException>();

        var stepFault = finalReport.InnerException.ShouldBeOfType<FaultHubException>();

        stepFault.Context.BoundaryName.ShouldBe(nameof(MockParsePageThatFails));
    }
    
    [SuppressMessage("ReSharper", "UnusedParameter.Local")]
    private IObservable<string> Step1_Fails(string input) =>
        Observable.Throw<string>(new InvalidOperationException("Failure in Step 1"));

    [SuppressMessage("ReSharper", "UnusedParameter.Local")]
    private IObservable<Unit> Step2_Succeeds(string input) => Observable.Return(Unit.Default);

    [Test]
    public async Task AsStep_Attributes_Assigned_To_The_Correct_Step() {
        var service = new ExternalService();

        var transaction = service.ExecuteWorkflow(
                input => Step1_Fails(input).AsStep(),
                result => Step2_Succeeds(result).AsStep()
            )
            .BeginWorkflow()
            .RunFailFast();

        await transaction.PublishFaults().Capture();

        BusEvents.Count.ShouldBe(1);
        var finalReport = BusEvents.Single().ShouldBeOfType<TransactionAbortedException>();

        var stepFault = finalReport.InnerException.ShouldBeOfType<FaultHubException>();

        stepFault.Context.BoundaryName.ShouldBe(nameof(Step1_Fails));
        finalReport.InnerException.InnerException.ShouldBeOfType<InvalidOperationException>();
    }
    
    [Test]
    public async Task Ambient_Context_Is_Overridden_By_Nested_Workflow_And_Not_Restored() {
        string contextInOuterScopeAfterInnerFailure = "CONTEXT_NOT_SET";

        IObservable<Unit> InnerFailingWorkflow() {
            return Observable.Return(Unit.Default)
                .BeginWorkflow("InnerWorkflow")
                .Then(_ => Observable.Throw<Unit>(new InvalidOperationException("Inner Failure")))
                .RunFailFast()
                .ToUnit();
        }

        await Observable.Return(Unit.Default)
            .BeginWorkflow("OuterWorkflow")
            .Then(_ => InnerFailingWorkflow().Catch(Observable.Empty<Unit>()))
            .Then(_ => {
                contextInOuterScopeAfterInnerFailure = Transaction.Current?.Name;
                return Observable.Return(Unit.Default);
            })
            .RunToEnd();

        

        contextInOuterScopeAfterInnerFailure.ShouldBe("OuterWorkflow");
    }
    
    [Test]
    public async Task AsStep_Correlates_Failures_With_RunToEnd() {
        var step2WasExecuted = false;

        var transaction = Observable.Return("start")
            .BeginWorkflow()
            .Then(_ => Step1_Fails("start").AsStep())
            .Then(results => {
                step2WasExecuted = true;
                return Step2_Succeeds(results.FirstOrDefault()).AsStep().Select(u => (object)u);
            })
            .RunToEnd();

        var result = await transaction.PublishFaults().Capture();

        result.Error.ShouldBeNull();
        step2WasExecuted.ShouldBeTrue("The second step should execute in RunAndCollect mode.");
        
        BusEvents.Count.ShouldBe(1);
    
        var finalFault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
        var aggregate = finalFault.InnerException.ShouldBeOfType<AggregateException>();
        var stepFault = aggregate.InnerExceptions.Single().ShouldBeOfType<FaultHubException>();

        stepFault.Context.BoundaryName.ShouldBe(nameof(Step1_Fails));
        stepFault.InnerException.ShouldBeOfType<InvalidOperationException>(); 
    }
    
    [Test]
    public async Task AsStep_Correlates_Failures_With_RunAndCollect() {
        var step2WasExecuted = false;

        var transaction = Observable.Return("start")
            .BeginWorkflow()
            .Then(_ => Step1_Fails("start").AsStep())
            .Then(results => {
                step2WasExecuted = true;
                return Step2_Succeeds(results.FirstOrDefault()).AsStep().Select(u => (object)u);
            })
            .RunAndCollect(Observable.Return);

        var result = await transaction.PublishFaults().Capture();

        result.Error.ShouldBeNull();
        step2WasExecuted.ShouldBeTrue("The second step should execute in RunAndCollect mode.");
        
        BusEvents.Count.ShouldBe(1);
        var finalFault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
        var aggregate = finalFault.InnerException.ShouldBeOfType<AggregateException>();
        var stepFault = aggregate.InnerExceptions.Single().ShouldBeOfType<FaultHubException>();

        stepFault.Context.BoundaryName.ShouldBe(nameof(Step1_Fails));
    }
    
    [Test]
    public async Task AsStep_With_NonCritical_Predicate_Suppresses_Error_And_Allows_Transaction_To_Continue() {
        var step2WasExecuted = false;

        var transaction = Observable.Return("start")
            .BeginWorkflow()
            .Then(_ => Observable.Throw<string>(new InvalidOperationException("This is a non-critical failure."))
                .AsStep(isNonCritical: ex => ex is InvalidOperationException))
            .Then(results => {
                step2WasExecuted = true;
                results.ShouldBeEmpty();
                return Step2_Succeeds(results.FirstOrDefault()).AsStep().Select(u => (object)u);
            })
            .RunToEnd();

        var result = await transaction.PublishFaults().Capture();

        step2WasExecuted.ShouldBeTrue("The transaction should have continued to the second step.");
        result.Error.ShouldBeNull("The final transaction should have completed successfully from the subscriber's perspective.");
            
        BusEvents.Count.ShouldBe(1);
        var finalFault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
        var aggregate = finalFault.InnerException.ShouldBeOfType<AggregateException>();
        var stepFault = aggregate.InnerExceptions.Single().ShouldBeOfType<FaultHubException>();
        stepFault.InnerException.ShouldBeOfType<InvalidOperationException>();
    }

    [Test]
    public async Task AsStep_With_NonCritical_Predicate_Continues_On_RunFailFast_And_Fails_At_The_End() {
        var step2WasExecuted = false;

        var transaction = Observable.Return("start")
            .BeginWorkflow()
            .Then(_ => Observable.Throw<string>(new InvalidOperationException("This is a non-critical failure."))
                .AsStep(isNonCritical: ex => ex is InvalidOperationException))
            .Then(results => {
                step2WasExecuted = true;
                return Step2_Succeeds(results.FirstOrDefault()).AsStep().Select(u => (object)u);
            })
            .RunFailFast();

        await transaction.PublishFaults().Capture();

        step2WasExecuted.ShouldBeTrue("The transaction should have continued to the second step.");

        BusEvents.Count.ShouldBe(1);
        var finalException = BusEvents.Single().ShouldBeOfType<FaultHubException>();
        finalException.ShouldNotBeOfType<TransactionAbortedException>();
        finalException.Message.ShouldContain("completed with non-critical errors");
            
        var aggregateException = finalException.InnerException.ShouldBeOfType<AggregateException>();
        var fault = aggregateException.InnerExceptions.Single().ShouldBeOfType<FaultHubException>();
        fault.InnerException.ShouldBeOfType<InvalidOperationException>();
    }

    [Test]
    public async Task AsStep_With_NonCritical_Predicate_Allows_RunFailFast_To_Continue_And_Aggregate() {
        var step2WasExecuted = false;
        IObservable<string> Step2Succeeds() => Observable.Return("Step 2 Succeeded");

        var transaction = Observable.Return("start")
            .BeginWorkflow()
            .Then(_ => Observable.Throw<string>(new InvalidOperationException("This is a non-critical failure."))
                .AsStep(isNonCritical: ex => ex is InvalidOperationException))
            .Then(results => {
                step2WasExecuted = true;
                results.ShouldBeEmpty();
                return Step2Succeeds().AsStep().Select(u => (object)u);
            })
            .RunFailFast();

        await transaction.PublishFaults().Capture();

        step2WasExecuted.ShouldBeTrue("The transaction should have continued to the second step.");

        BusEvents.Count.ShouldBe(1);
        var finalException = BusEvents.Single().ShouldBeOfType<FaultHubException>();
        finalException.ShouldNotBeOfType<TransactionAbortedException>();
        finalException.Message.ShouldContain("completed with non-critical errors");

        var aggregateException = finalException.InnerException.ShouldBeOfType<AggregateException>();
        var fault = aggregateException.InnerExceptions.Single().ShouldBeOfType<FaultHubException>();
        fault.InnerException.ShouldBeOfType<InvalidOperationException>();
        fault.Context.Tags.ShouldContain(Transaction.NonCriticalStepTag);
    }

}