using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.FaultHub;
using Xpand.Extensions.Reactive.FaultHub.Transaction;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Tests.FaultHubTests.TransactionalApi;
[TestFixture]
public class AsStepTests : FaultHubTestBase {
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

        var transaction = Observable.Return("start")
            .BeginWorkflow("ServiceWorkflow")
            .Then(_ => service.WhenVisitPage(
                driver => MockParsePageThatFails(driver).AsStep(), parseResult => MockBrowserSelector(parseResult).AsStep()
            ))
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
    public async Task AsStep_OnFault_Critical_Aborts_Transaction() {
        var step2WasExecuted = false;

        var transaction = Observable.Return("start")
            .BeginWorkflow()
            .Then(_ => Observable.Throw<string>(new InvalidOperationException("Critical Failure"))
                .AsStep(onFault: _ => ResilienceAction.Critical))
            .Then(results => {
                step2WasExecuted = true;
                return Step2_Succeeds(results.FirstOrDefault()).AsStep().Select(u => (object)u);
            })
            .RunFailFast();

        await transaction.PublishFaults().Capture();

        step2WasExecuted.ShouldBeFalse();
        BusEvents.Single().ShouldBeOfType<TransactionAbortedException>();
    }
    
    [Test]
public async Task AsStep_OnFault_Tolerate_Continues_Transaction_And_Aggregates_Report() {
    var step2WasExecuted = false;

    var transaction = Observable.Return("start")
        .BeginWorkflow()
        .Then(_ => Observable.Throw<string>(new InvalidOperationException("Tolerated Failure"))
            .AsStep(onFault: _ => ResilienceAction.Tolerate))
        .Then(results => {
            step2WasExecuted = true;
            return Step2_Succeeds(results.FirstOrDefault()).AsStep().Select(u => (object)u);
        })
        .RunFailFast();

    await transaction.PublishFaults().Capture();

    step2WasExecuted.ShouldBeTrue();
    var finalReport = BusEvents.Single().ShouldBeOfType<FaultHubException>();
    finalReport.ShouldNotBeOfType<TransactionAbortedException>();
    var stepFault = finalReport.InnerException.ShouldBeOfType<AggregateException>()
        .InnerExceptions.Single().ShouldBeOfType<FaultHubException>();
    stepFault.Context.Tags.ShouldContain(Transaction.NonCriticalStepTag);
}

[Test]
public async Task AsStep_OnFault_Suppress_Continues_Transaction_And_Aggregates_Report() {
    var step2WasExecuted = false;

    var transaction = Observable.Return("start")
        .BeginWorkflow()
        .Then(_ => Observable.Throw<string>(new InvalidOperationException("Suppressed Failure"))
            .AsStep(onFault: _ => ResilienceAction.Suppress))
        .Then(results => {
            step2WasExecuted = true;
            results.ShouldBeEmpty();
            return Step2_Succeeds(results.FirstOrDefault()).AsStep().Select(u => (object)u);
        })
        .RunToEnd();

    var result = await transaction.PublishFaults().Capture();

    step2WasExecuted.ShouldBeTrue();
    result.Error.ShouldBeNull();
    
    var finalReport = BusEvents.Single().ShouldBeOfType<FaultHubException>();
    var stepFault = finalReport.InnerException.ShouldBeOfType<AggregateException>()
        .InnerExceptions.Single().ShouldBeOfType<FaultHubException>();
    stepFault.InnerException.ShouldBeOfType<InvalidOperationException>();
    stepFault.Context.Tags.ShouldContain(Transaction.NonCriticalStepTag);
}

[Test]
public async Task AsStep_Outside_Transaction_Rethrows_Original_Exception() {
    var originalException = new InvalidOperationException("Raw Failure");
    var stream = Observable.Throw<Unit>(originalException)
        .AsStep(onFault: _ => ResilienceAction.Suppress);

    var result = await stream.Capture();

    result.Error.ShouldBeSameAs(originalException);
}
    [Test]
    public async Task AsStep_Attributes_Assigned_To_The_Correct_Step() {
        var service = new ExternalService();

        var transaction = Observable.Return("start")
            .BeginWorkflow("ServiceWorkflow")
            .Then(_ => service.ExecuteWorkflow(
                input => Step1_Fails(input).AsStep(), result => Step2_Succeeds(result).AsStep()
            ))
            .RunFailFast();

        await transaction.PublishFaults().Capture();

        BusEvents.Count.ShouldBe(1);
        var finalReport = BusEvents.Single().ShouldBeOfType<TransactionAbortedException>();

        var stepFault = finalReport.InnerException.ShouldBeOfType<FaultHubException>();

        stepFault.Context.BoundaryName.ShouldBe(nameof(Step1_Fails));
        finalReport.InnerException.InnerException.ShouldBeOfType<InvalidOperationException>();
    }
    
    [Test]
    public async Task Ambient_Context_Is_Restored_After_Nested_Workflow_Completes() {
        IObservable<Unit> InnerFailingWorkflow() {
            return Observable.Return(Unit.Default)
                .BeginWorkflow("InnerWorkflow")
                .Then(_ => Observable.Throw<Unit>(new InvalidOperationException("Inner Failure")))
                .RunFailFast()
                .ToUnit();
        }

        IObservable<Unit> StepAfterInnerFailure() 
            => Observable.Throw<Unit>(new InvalidOperationException("Failure after nested completion"));

        var transaction = Observable.Return(Unit.Default)
            .BeginWorkflow("OuterWorkflow")
            .Then(_ => InnerFailingWorkflow().Catch(Observable.Empty<Unit>()))
            .Then(_ => StepAfterInnerFailure().AsStep())
            .RunToEnd();

        await transaction.PublishFaults().Capture();

        BusEvents.Count.ShouldBe(1);
        var finalFault = BusEvents.Single().ShouldBeOfType<FaultHubException>();

        var tree = finalFault.OperationTree();
        tree.ShouldNotBeNull();
        tree.Name.ShouldBe("OuterWorkflow");

        var stepNode = tree.Children.ShouldHaveSingleItem();
        stepNode.Name.ShouldBe(nameof(StepAfterInnerFailure));
        stepNode.GetRootCause().ShouldBeOfType<InvalidOperationException>()
            .Message.ShouldBe("Failure after nested completion");
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
    public async Task AsStep_Receives_Transaction_Context_Across_Async_Boundary() {
        var transaction = Observable.Return("start")
            .BeginWorkflow("MyTestWorkflow")
            .Then(_ =>
                Observable.Timer(TimeSpan.FromMilliseconds(20))
                    .SelectMany(_ => Observable.Throw<string>(new InvalidOperationException("Deliberate inner failure"))
                        .AsStep()
                    )
                    .ContinueOnFault()
            )
            .RunFailFast();

        await transaction.Capture();

        BusEvents.Count.ShouldBe(1);
        var publishedFault = BusEvents.Single();

        publishedFault.InnerException.ShouldBeOfType<FaultHubException>(
            "The exception caught by the resilience operator should have been a FaultHubException created by AsStep. " +
            "If it was the original InvalidOperationException, it means Transaction.Current was null and the context was lost."
        );
    }
    
    [Test]
    public async Task AsStep_Receives_Transaction_Context_Across_Forced_Scheduler_Boundary() {
        var transaction = Observable.Return("start")
            .BeginWorkflow("MyTestWorkflow")
            .Then(_ => Observable.Return(Unit.Default)
                    .SubscribeOn(TaskPoolScheduler.Default)
                    .SelectMany(_ =>
                        Observable.Throw<string>(new InvalidOperationException("Deliberate inner failure"))
                            .AsStep()
                    )
                    .ContinueOnFault()
            )
            .RunFailFast();

        await transaction.Capture();

        BusEvents.Count.ShouldBe(1);
        var publishedFault = BusEvents.Single();

        publishedFault.InnerException.ShouldBeOfType<FaultHubException>(
            "The exception caught by the resilience operator should have been a FaultHubException created by AsStep. " +
            "If it was the original InvalidOperationException, it means Transaction.Current was null and the context was lost."
        );
    }
    
    [Test]
        public async Task TransactionContext_Is_Preserved_When_Inner_Step_Uses_ChainFaultContext_Across_Async_Boundary() {
            Exception observedException = null;

            var transaction = Observable.Return("start")
                .BeginWorkflow("MyTestWorkflow")
                .Then(_ => Observable.Timer(TimeSpan.FromMilliseconds(20))
                        .SelectMany(_ => {
                            if (Transaction.Current == null) {
                                return Observable.Throw<string>(new InvalidOperationException(
                                    "Transaction.Current was NULL inside ChainFaultContext."));
                            }
                            return Observable.Return("Success");
                        })
                        .ChainFaultContext()
                )
                .RunFailFast();

            try {
                await transaction;
            }
            catch (Exception ex) {
                observedException = ex;
            }

            observedException.ShouldBeNull(
                "The transaction failed because the TransactionContext was lost. " +
                "This proves a conflict between the PushStackFrame and ChainFaultContext guardians."
            );
        }

    [Test]
    public async Task AsStep_Loses_Context_When_Transaction_Is_Nested_Inside_Outer_Resilience_Boundary() {
        var transaction = Observable.Return("start")
            .ChainFaultContext(["OuterBoundary"])
            .SelectMany(_ => Observable.Return("inner")
                    .BeginWorkflow("InnerWorkflow")
                    .Then(_ => Observable.Timer(TimeSpan.FromMilliseconds(20))
                            .SelectMany(_ => Observable.Throw<string>(new InvalidOperationException("Inner Failure")).AsStep())
                    )
                    .RunFailFast()
            )
            .ContinueOnFault();

        await transaction.Capture();

        BusEvents.Count.ShouldBe(1);
        var publishedFault = BusEvents.Single();

        var abortedException = publishedFault.InnerException.ShouldBeOfType<TransactionAbortedException>();
        var stepFault = abortedException.InnerException.ShouldBeOfType<FaultHubException>();

        stepFault.Context.Tags.ShouldContain(Transaction.AsStepOriginTag,
            "The fault from the failing step was not tagged by AsStep(), which proves that the " +
            "transaction context was lost before AsStep could process the exception.");
    }

}
    






