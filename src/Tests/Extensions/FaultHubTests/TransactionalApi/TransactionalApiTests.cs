using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Numeric;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Tests.FaultHubTests.TransactionalApi {
    public class TransactionalApiTests  : FaultHubTestBase {
        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<string> FailingOperation(SubscriptionCounter failingCounter)
            => Observable.Throw<string>(new InvalidOperationException("Operation Failed"))
                .TrackSubscriptions(failingCounter)
                .PushStackFrame();
        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> SuccessfulOperation(SubscriptionCounter successfulCounter)
            => Unit.Default.Observe()
                .TrackSubscriptions(successfulCounter)
                .PushStackFrame();
        [Test]
        public async Task Inner_Operation_Failure_Triggers_Outer_Transactional_Retry() {
            var failingCounter = new SubscriptionCounter();
            var successfulCounter = new SubscriptionCounter();
            var outerCounter = new SubscriptionCounter();

            var resilientFailingOperation = FailingOperation(failingCounter).Retry(2);
            var successfulOperation = SuccessfulOperation(successfulCounter);
            var result = await resilientFailingOperation
                .BeginWorkflow( "MyTransaction", context: ["MyCustomContext"])
                .Then(successfulOperation)
                .RunToEnd()
                .TrackSubscriptions(outerCounter)
                .ChainFaultContext(s => s.Retry(3), ["Outer"])
                .PublishFaults().Capture();

            result.IsCompleted.ShouldBe(true);
            result.Error.ShouldBeNull();
            BusEvents.Count.ShouldBe(1);
            (failingCounter.Count, successfulCounter.Count, outerCounter.Count).ShouldBe((6, 3, 3));
            
            var finalFault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            finalFault.AllContexts.ShouldContain("Outer");
            finalFault.AllContexts.ShouldContain("MyTransaction");
            
            var aggregateException = finalFault.InnerException.ShouldBeOfType<FaultHubException>()
                .InnerException.ShouldBeOfType<AggregateException>();
            var innerFault = aggregateException.InnerExceptions.Single().ShouldBeOfType<FaultHubException>();

            innerFault.AllContexts.ShouldContain("MyTransaction");
            innerFault.AllContexts.ShouldContain("resilientFailingOperation");
        }

        [Test]
        public async Task SequentialTransaction_Aborts_On_First_Failure_When_FailFast_Is_True() {
            await Observable.Return(Unit.Default)
                .BeginWorkflow("FailFastTransaction")
                .Then(Observable.Throw<string>(new InvalidOperationException("Failing Operation")))
                .Then(Observable.Return(Unit.Default))
                .RunFailFast()
 
               .PublishFaults().Capture();

            var finalFault = BusEvents.Single().ShouldBeOfType<TransactionAbortedException>();
            finalFault.Context.BoundaryName.ShouldBe("FailFastTransaction");

            var originalFault = finalFault.InnerException.ShouldBeOfType<FaultHubException>();
            originalFault.InnerException.ShouldBeOfType<InvalidOperationException>().Message
                .ShouldBe("Failing Operation");
        }
        [Test]
        public async Task Can_Create_Transaction_From_IEnumerable_Of_Observables() {
            var operations = new[] { "Success1", "Failure1", "Success2" }
                .Select(item => item.StartsWith("Failure")
                    ? Observable.Throw<string>(new InvalidOperationException(item))
                    
                    : Observable.Return(item));

            var result = await operations
                .BeginWorkflow( "FromIEnumerableTx",TransactionMode.Sequential)
                .RunToEnd()
                .PublishFaults()
                .Capture();
            result.IsCompleted.ShouldBe(true);
            result.Items.ShouldBeEmpty();

            BusEvents.Count.ShouldBe(1);
            var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            var aggregate = fault.InnerException.ShouldBeOfType<AggregateException>();
            var innerFault = aggregate.InnerExceptions.Single().ShouldBeOfType<FaultHubException>();

            innerFault.InnerException.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Failure1");

            
            innerFault.AllContexts.ShouldContain($"{nameof(operations)}[1]");
        }

        [Test]
        public async Task NestedFailFastTransaction_Aborts_Outer_FailFastTransaction() {
            var innerTx1 = Observable.Return(Unit.Default)
                .BeginWorkflow("InnerTransaction1")
                .Then(Observable.Throw<Unit>(new InvalidOperationException("Inner Tx1 Failed")))
                .RunFailFast();
            var innerTx2 = Observable.Return(Unit.Default)
                .BeginWorkflow("InnerTransaction2")
                .RunFailFast();
            await innerTx1
                .BeginWorkflow("OuterTransaction")
                .Then(innerTx2)
                .RunFailFast()
                .PublishFaults().Capture();
            var finalFault = BusEvents.Single().ShouldBeOfType<TransactionAbortedException>();
            finalFault.Context.BoundaryName.ShouldBe("OuterTransaction");

            var innerFault = finalFault.InnerException.ShouldBeOfType<TransactionAbortedException>();
            innerFault.Context.BoundaryName.ShouldBe("InnerTransaction1");



            var originalOpFault = innerFault.InnerException.ShouldBeOfType<TransactionAbortedException>().InnerException.ShouldBeOfType<FaultHubException>();
            originalOpFault.InnerException.ShouldBeOfType<InvalidOperationException>().Message
                .ShouldBe("Inner Tx1 Failed");
            originalOpFault.AllContexts.ShouldContain(ctx => ctx is string && ((string)ctx).StartsWith("InnerTransaction1 -"));
        }

        [Test]
        public async Task NestedRunToCompletionTransaction_Fails_OuterFailFastTransaction_After_Completion() {
            
            var innerTx1 = Observable.Return(Unit.Default)
                .BeginWorkflow("InnerTransaction1")
                .RunFailFast();
            var innerTx2 = Observable.Return(Unit.Default)
                .BeginWorkflow("InnerTransaction2")
                .Then(Observable.Throw<Unit>(new InvalidOperationException("Inner Tx2 Failed")))
                .RunToEnd();
            await innerTx1
                .BeginWorkflow("OuterTransaction")
                .Then(innerTx2)
                .RunFailFast()
                .PublishFaults().Capture();
            var finalFault = BusEvents.Single().ShouldBeOfType<TransactionAbortedException>();
            finalFault.Context.BoundaryName.ShouldBe("OuterTransaction");

            var stepFault = finalFault.InnerException.ShouldBeOfType<FaultHubException>();
            stepFault.Context.BoundaryName.ShouldBe("innerTx2");

            stepFault.Message.ShouldBe("InnerTransaction2 completed with errors");
            var aggregateException = stepFault.InnerException.ShouldBeOfType<FaultHubException>()
                .InnerException.ShouldBeOfType<AggregateException>();
            var originalOpFault = aggregateException.InnerExceptions.Single().ShouldBeOfType<FaultHubException>();
            originalOpFault.InnerException.ShouldBeOfType<InvalidOperationException>().Message
                .ShouldBe("Inner Tx2 Failed");
            finalFault.AllContexts.ShouldContain(o => o is string && ((string)o).StartsWith("OuterTransaction -"));
            originalOpFault.AllContexts.ShouldContain(o => o is string && ((string)o).StartsWith("InnerTransaction2 -"));
        }
        [Test]
        public async Task RunToCompletionOuterTransaction_Aggregates_Failures_From_Nested_Transactions() {
            
            var innerTx1 = Observable.Throw<Unit>(new InvalidOperationException("Inner Tx1 Failed"))
                .BeginWorkflow( "InnerTransaction1")
                .RunFailFast();
            var innerTx2 = Observable.Throw<Unit>(new InvalidOperationException("Inner Tx2 Failed"))
                .BeginWorkflow( "InnerTransaction2")
                .RunToEnd();
            await innerTx1
                .BeginWorkflow( "OuterTransaction")
                .Then(innerTx2)
                .RunToEnd()
                .PublishFaults().Capture();
            var finalFault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            finalFault.Message.ShouldBe("OuterTransaction completed with errors");

            var aggregateException = finalFault.InnerException.ShouldBeOfType<AggregateException>();
            aggregateException.InnerExceptions.Count.ShouldBe(2);

            var failure1 = aggregateException.InnerExceptions.OfType<TransactionAbortedException>().Single();
            failure1.Message.ShouldBe("InnerTransaction1 failed");
            failure1.InnerException.ShouldBeOfType<TransactionAbortedException>().InnerException.ShouldBeOfType<FaultHubException>()
                .InnerException.ShouldBeOfType<InvalidOperationException>()
                .Message.ShouldBe("Inner Tx1 Failed");

            var failure2 = aggregateException.InnerExceptions.OfType<FaultHubException>()
                .Single(ex => ex is not TransactionAbortedException);
            failure2.Message.ShouldBe("InnerTransaction2 completed with errors");
            failure2.InnerException.ShouldBeOfType<FaultHubException>().InnerException.ShouldBeOfType<AggregateException>()
                .InnerExceptions.Single().ShouldBeOfType<FaultHubException>()
                .InnerException.ShouldBeOfType<InvalidOperationException>()
                .Message.ShouldBe("Inner Tx2 Failed");
        }
        public class HomePage { public string Url { get; set; } }
        


        [Test]
        public async Task Web_Scraping_Transaction_Fails_On_Last_Step() {
            var source = Observable.Return("Http://example.com");
            var result = await source.BeginWorkflow("WebScraping-Tx")
                .Then(Step2_GetHomePage)
                .Then(Step3_ExtractUrls)
                .Then(Step4_ProcessUrls)
                .RunFailFast()
                .PublishFaults()
                
                .Capture();

            result.IsCompleted.ShouldBe(true);
            BusEvents.Count.ShouldBe(1);
            
            var abortedException = BusEvents.Single().ShouldBeOfType<TransactionAbortedException>();
            abortedException.Context.BoundaryName.ShouldBe("WebScraping-Tx");

            var finalFault = abortedException.InnerException.ShouldBeOfType<FaultHubException>();
            finalFault.InnerException.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("URL processing failed");
            
            finalFault.LogicalStackTrace.Count().ShouldBe(4);
            finalFault.AllContexts.ShouldContain(nameof(Step4_ProcessUrls));
            finalFault.AllContexts.ShouldContain("WebScraping-Tx");
        }
        IObservable<HomePage> Step2_GetHomePage(string[] urls) => Observable.Return(new HomePage { Url = urls.Single() });
        IObservable<List<Url>> Step3_ExtractUrls(HomePage[] c) => Observable.Return(new List<Url> { new() { Href = "/page1" } });
        IObservable<Unit> Step4_ProcessUrls(List<Url>[] o) => Observable.Throw<Unit>(new InvalidOperationException("URL processing failed"));
        
        [Test]
        public async Task RunToCompletion_Executes_All_Steps_And_Aggregates_Failures() {
            var part1Counter = 0;
            var part2Counter = 0;
            var part3Counter = 0;
            var part4Counter = 0;
            var source = Observable.Return("http://example.com")
                .Do(_ => part1Counter++);
            await source.BeginWorkflow("RunToCompletion-Tx")
                .Then(Step2GetHomePageFails)
                .Then(Step3ExtractUrlsEmpty)
                .Then(Step4ProcessUrlsFails)
                .RunToEnd()
                .PublishFaults()
                .Capture();
            (part1Counter, part2Counter, part3Counter, part4Counter).ShouldBe((1, 1, 1, 1));

            BusEvents.Count.ShouldBe(1);
            var finalFault = BusEvents.Single().ShouldBeOfType<FaultHubException>();

            var aggregate = finalFault.InnerException.ShouldBeOfType<AggregateException>();
            aggregate.InnerExceptions.Count.ShouldBe(2);
            var fault2 = aggregate.InnerExceptions.OfType<FaultHubException>()
                .FirstOrDefault(ex => ex.AllContexts.Contains($"RunToCompletion-Tx - {nameof(Step2GetHomePageFails)}"));
            fault2.ShouldNotBeNull();
            fault2.InnerException.ShouldBeOfType<InvalidOperationException>().Message
                .ShouldBe("Homepage lookup failed");
            var fault4 = aggregate.InnerExceptions.OfType<FaultHubException>()
                .FirstOrDefault(ex => ex.AllContexts.Contains($"RunToCompletion-Tx - {nameof(Step4ProcessUrlsFails)}"));
            fault4.ShouldNotBeNull();
            fault4.InnerException.ShouldBeOfType<InvalidOperationException>().Message
                .ShouldBe("URL processing failed");
            
            IObservable<HomePage> Step2GetHomePageFails(string[] ints) {
                part2Counter++;
                return Observable.Throw<HomePage>(new InvalidOperationException("Homepage lookup failed"));
            }

            IObservable<List<Url>> Step3ExtractUrlsEmpty(HomePage[] customer) {
                part3Counter++;
                customer.ShouldBeEmpty();
                return Observable.Return(new List<Url>());
            }

            IObservable<Unit> Step4ProcessUrlsFails(List<Url>[] _) {
                part4Counter++;
                return Observable.Throw<Unit>(new InvalidOperationException("URL processing failed"));
            }
        }        
        [Test]
        public async Task Fluent_Builder_Succeeds() {
            var source = Observable.Range(1, 3);
            IObservable<int> SecondStreamSelector(IList<int> results) => Observable.Return(results.Sum());

            var result = await source
                .BeginWorkflow("TwoPartTx")
                .Then(SecondStreamSelector)
                .RunFailFast()
                .Capture();
            result.Items.SelectArray().Single().ShouldBe(6);
            BusEvents.ShouldBeEmpty();
        }

        [Test]
        public async Task Fluent_Builder_Fails_If_First_Part_Fails() {
            var secondPartStarted = false;
            var source = Observable.Throw<int>(new InvalidOperationException("Failure in Part 1"));

            await source
                .BeginWorkflow("TwoPartTxFailure1")
                .Then(SecondStreamSelector)
                .RunFailFast()
                .PublishFaults()
                .Capture();
            secondPartStarted.ShouldBeFalse();

            var abortedException = BusEvents.Single().ShouldBeOfType<TransactionAbortedException>();
            abortedException.Context.BoundaryName.ShouldBe("TwoPartTxFailure1");
            
            var fault = abortedException.InnerException.ShouldBeOfType<FaultHubException>();
            fault.InnerException.ShouldBeOfType<InvalidOperationException>().Message.ShouldStartWith("Failure in Part 1");

            fault.AllContexts.ShouldContain("TwoPartTxFailure1 - source");
            IObservable<Unit> SecondStreamSelector(int[] _) {
                secondPartStarted = true;
                return Observable.Empty<Unit>();
            }

        }
        
        [Test]
        public async Task Fluent_Builder_Handles_Sync_Exception_In_Then_Selector() {
            IObservable<string> source = Observable.Return<IList<string>>(new List<string>())
                .SelectMany(_ => Observable.Throw<string>(new Exception("Outer")));
            var part3Executed = false;

            var result = await source.BeginWorkflow("SyncException-Tx")
                .Then(lists => lists.DeferAction(() => throw new NotImplementedException())
                        .ConcatDeferToUnit(() => Observable.Return("This part is never reached")), stepName: "First")
                .Then(_ => {
                    
                    part3Executed = true;
                        return Observable.Return(Unit.Default);
                    }, stepName: "Second")
                .RunToEnd()
                .PublishFaults()
               
                 .Capture();

            result.IsCompleted.ShouldBe(true);
            result.Error.ShouldBeNull();
            part3Executed.ShouldBeTrue();

            BusEvents.Count.ShouldBe(1);
            var finalFault = BusEvents.Single().ShouldBeOfType<FaultHubException>();

            var aggregate = finalFault.InnerException.ShouldBeOfType<AggregateException>();
            aggregate.InnerExceptions.Count.ShouldBe(2);
            var innerFault = aggregate.InnerExceptions
                .OfType<FaultHubException>()
                .FirstOrDefault(f => f.InnerException is NotImplementedException);
            innerFault.ShouldNotBeNull();
            innerFault.AllContexts.ShouldContain("SyncException-Tx","First");
        }    
        [Test]
        public async Task Fluent_Builder_RunToCompletion_When_First_Part_Fails() {
            var part1Counter = 0;
            var part2Counter = 0;
    
            var source = Observable.Throw<int>(new InvalidOperationException("Part 1 Always Fails"))
                .Do(_ => part1Counter++, _ => part1Counter++);
            IObservable<int> SecondStreamSelector(int[] results) {
                part2Counter++;
                results.ShouldBeEmpty();
                return Observable.Return(999);
            }

            var result = await source
                .BeginWorkflow("RunToCompletionTx")
                .Then(SecondStreamSelector)
                .RunToEnd()
                .PublishFaults()
                .Capture();
            (part1Counter, part2Counter).ShouldBe((1, 1));

            result.IsCompleted.ShouldBe(true);
            result.Items.SelectArray().ShouldBeEmpty();
            var finalFault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            var aggregate = finalFault.InnerException.ShouldBeOfType<AggregateException>();
            var innerFault = aggregate.InnerExceptions.Single().ShouldBeOfType<FaultHubException>();
            innerFault.InnerException.ShouldBeOfType<InvalidOperationException>().Message
                .ShouldBe("Part 1 Always Fails");
            innerFault.AllContexts.ShouldContain("RunToCompletionTx");
            innerFault.AllContexts.ShouldContain(nameof(source));
        }    
        [Test]
        public async Task Fluent_Builder_Uses_Fallback_On_Failure_And_Continues_Transaction() {
            var primarySelectorCalled = false;
            var fallbackSelectorCalled = false;
            var finalStepReceivedCorrectData = false;

            var source = Observable.Return(123);
            var result = await source.BeginWorkflow("Fallback-Tx")
                .Then(FailingStep, fallbackSelector: FallbackStep)
                .Then(FinalStep)
                .RunToEnd()
                .PublishFaults()
                .Capture();
            result.IsCompleted.ShouldBe(true);
            result.Error.ShouldBeNull();
            BusEvents.ShouldBeEmpty(); 

            primarySelectorCalled.ShouldBe(true);
            fallbackSelectorCalled.ShouldBe(true);
            finalStepReceivedCorrectData.ShouldBe(true);
            
            IObservable<string> FailingStep(int[] ints) {
                primarySelectorCalled = true;
                return Observable.Throw<string>(new InvalidOperationException("Primary step failed"));
            }

            IObservable<string> FallbackStep(Exception ex, int[] ints) {
                fallbackSelectorCalled = true;
                ex.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Primary step failed");
                return Observable.Return("Fallback Data");
            }

            IObservable<Unit> FinalStep(string[] inputs) {
                if (inputs.SingleOrDefault() == "Fallback Data") {
                    finalStepReceivedCorrectData = true;
                }
                return Unit.Default.Observe();
            }
        }

        [Test]
        public async Task Fluent_Builder_FailFast_Ignores_Fallback_And_Propagates_Error() {
            var primarySelectorCalled = false;
            var fallbackSelectorCalled = false;
            
            var source = Observable.Return(123);

            
            await source.BeginWorkflow("FailFast-Fallback-Tx")
                .Then(FailingStep, fallbackSelector:FallbackStep)
                .RunFailFast()
                .PublishFaults()
                .Capture();
            primarySelectorCalled.ShouldBe(true);
            fallbackSelectorCalled.ShouldBe(false);

            BusEvents.Count.ShouldBe(1);

            var abortedException = BusEvents.Single().ShouldBeOfType<TransactionAbortedException>();
            abortedException.Context.BoundaryName.ShouldBe("FailFast-Fallback-Tx");

            var fault = abortedException.InnerException.ShouldBeOfType<FaultHubException>();
            fault.InnerException.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Primary step failed");
            
            fault.LogicalStackTrace.Count().ShouldBe(2);
    
            fault.AllContexts.ShouldContain("FailFast-Fallback-Tx - FailingStep");
            IObservable<string> FailingStep(int[] ints) {
                primarySelectorCalled = true;
                return Observable.Throw<string>(new InvalidOperationException("Primary step failed"));
            }

            IObservable<string> FallbackStep(Exception ex, int[] ints) {
                fallbackSelectorCalled = true;
                return Observable.Return("Fallback Data");
            }

        }
        [Test]
        public async Task RunToEnd_TopLevel_Transaction_Fails_Atomically_And_Discards_Partial_Results() {
            var source = Observable.Return("Initial Value");
            var result = await source.BeginWorkflow( "TopLevel-Atomic-Tx")
                .Then(Step2Succeeds)
                .Then(Step3Fails)
                .RunToEnd()
                .PublishFaults()
                .Capture();
            result.IsCompleted.ShouldBe(true);
            result.Items.ShouldBeEmpty("Partial results should be discarded in a failed top-level transaction.");

            BusEvents.Count.ShouldBe(1);
            var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            fault.AllContexts.ShouldContain("TopLevel-Atomic-Tx");
            var aggregate = fault.InnerException.ShouldBeOfType<AggregateException>();
            var innerFault = aggregate.InnerExceptions.Single().ShouldBeOfType<FaultHubException>();
            innerFault.InnerException.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Step 3 Failed");
            innerFault.AllContexts.ShouldContain("TopLevel-Atomic-Tx - Step3Fails");
            innerFault.AllContexts.ShouldNotContain("TopLevel-Atomic-Tx - Step2Succeeds");
            
            
            innerFault.LogicalStackTrace.Count().ShouldBe(3);
        }
        IObservable<string> Step2Succeeds(string[] _) => Observable.Return("Step 2 Succeeded");

        IObservable<string> Step3Fails(string[] _)
            => Observable.Throw<string>(new InvalidOperationException("Step 3 Failed"));
        [TestCase]
        public async Task Nested_RunToEnd_Feeds_Next_Step_With_Partial_Results_Before_Propagating_Failure() {
            var step1Succeeded = false;
            var finalStepWasCalled = false;
            var dataReceivedInFinalStep = new List<string>();
            
            var outerTransactionStream = Observable.Return("Outer Initial")
                .BeginWorkflow("Outer-Tx")
                .Then(_ => InnerFailingTransaction())
                .Then(partialResults => {
                    finalStepWasCalled = true;
             
                    dataReceivedInFinalStep.AddRange(partialResults.SelectMany(s => s));
                    return Observable.Return(Unit.Default);
                })
                .RunToEnd();
            var result = await outerTransactionStream.Capture();

            step1Succeeded.ShouldBe(true, "The successful inner step should have executed.");
            finalStepWasCalled.ShouldBe(true, "The final step should have been called with the partial results.");
            dataReceivedInFinalStep.ShouldContain("Partial Success Data");
            result.Error.ShouldNotBeNull("The stream should have failed after processing partial results.");
            
            var outermostFault = result.Error.ShouldBeOfType<FaultHubException>();
            outermostFault.Message.ShouldBe("Outer-Tx completed with errors");
            var outerAggregate = outermostFault.InnerException.ShouldBeOfType<AggregateException>();
            var nestedTxStepFault = outerAggregate.InnerExceptions.Single().ShouldBeOfType<FaultHubException>();

            nestedTxStepFault.Message.ShouldBe("Nested-Tx completed with errors");
            nestedTxStepFault.AllContexts.ShouldContain("Outer-Tx - InnerFailingTransaction");

            var stepAggregate = nestedTxStepFault.InnerException.ShouldBeOfType<FaultHubException>()
                .InnerException.ShouldBeOfType<AggregateException>();
            var stepFault = stepAggregate.InnerExceptions.Single().ShouldBeOfType<FaultHubException>();
            stepFault.Message.ShouldBe("Inner Failure");
            stepFault.AllContexts.ShouldContain("Nested-Tx - InnerStep2Fails");
            
            IObservable<string[]> InnerFailingTransaction() {
                var innerSource = Observable.Return("Inner Initial");
                IObservable<string> InnerStep1Succeeds(string[] _) {
                    step1Succeeded = true;
                    return Observable.Return("Partial Success Data");
                }

                IObservable<string> InnerStep2Fails(string[] _)
                    => Observable.Throw<string>(new InvalidOperationException("Inner Failure"));
                return innerSource.BeginWorkflow("Nested-Tx")
                    .Then(InnerStep1Succeeds)
                    .Then(InnerStep2Fails)
                    .RunToEnd();
            }

        }
        
        public static IEnumerable<TestCaseData> Lambda_Naming_Cases() {
            ITransactionBuilder<Unit> ImplicitNameAction(ITransactionBuilder<string> builder) 
                => builder.Then(_ => Observable.Throw<Unit>(new InvalidOperationException("Lambda Failed")));
            var expectedImplicitName = @"Observable.Throw<Unit>";
            yield return new TestCaseData((Func<ITransactionBuilder<string>, ITransactionBuilder<Unit>>)ImplicitNameAction, expectedImplicitName).SetName("Implicit Lambda Name");
            ITransactionBuilder<Unit> ExplicitNameAction(ITransactionBuilder<string> builder)
                => builder.Then(_ => Observable.Throw<Unit>(new InvalidOperationException("Lambda Failed")), "MyExplicitLambdaName");
            var expectedExplicitName = "MyExplicitLambdaName";
            yield return new TestCaseData((Func<ITransactionBuilder<string>, ITransactionBuilder<Unit>>)ExplicitNameAction, expectedExplicitName).SetName("Explicit Lambda Name");
        }
        [Test]
        [TestCaseSource(nameof(Lambda_Naming_Cases))]
        public async Task Fluent_Builder_Correctly_Names_Failing_Lambda_Step(
            Func<ITransactionBuilder<string>, ITransactionBuilder<Unit>> configureTransaction,
            string expectedStepName) {
            var source = Observable.Return("Initial Value");
            var transactionBuilder = source.BeginWorkflow("Lambda-Naming-Tx");

            var configuredTransaction = configureTransaction(transactionBuilder);

            await configuredTransaction
                .RunToEnd()
                .PublishFaults()
                .Capture();
            BusEvents.Count.ShouldBe(1);
            var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();

            var aggregate = fault.InnerException.ShouldBeOfType<AggregateException>();
            var innerFault = aggregate.InnerExceptions.Single().ShouldBeOfType<FaultHubException>();
            

            var expectedContext = $"Lambda-Naming-Tx - {expectedStepName}";
            var actualContext = innerFault.AllContexts.OfType<string>()
                .FirstOrDefault(s => s.StartsWith("Lambda-Naming-Tx -"));
            actualContext.ShouldNotBeNull();

            var normalizedExpected = System.Text.RegularExpressions.Regex.Replace(expectedContext, @"\s", "");
            var normalizedActual = System.Text.RegularExpressions.Regex.Replace(actualContext, @"\s", "");

            normalizedActual.ShouldBe(normalizedExpected);
    
            innerFault.InnerException.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Lambda Failed");
        }

        [Test]
        public async Task RunToEnd_DoesNotExecuteNextStepTwice_WhenNestedStepEmitsAndFails() {
            var step3ExecutionCounter = 0;
            IObservable<string[]> Step2EmitsAndFails(object[] _)
                => Observable.Return(new[] { "Partial Data" })
                    .Concat(Observable.Throw<string[]>(new InvalidOperationException("Nested Step Failed")));
            IObservable<Unit> Step3Counter(string[][] _) {
                step3ExecutionCounter++;
                return Unit.Default.Observe();
            }

            var transaction = Observable.Return(Array.Empty<object>())
                .BeginWorkflow("Outer-Tx")
                .Then(Step2EmitsAndFails)
                .Then(Step3Counter)
                .RunToEnd();
            await transaction.PublishFaults().Capture();

            step3ExecutionCounter.ShouldBe(1, "The third step should only be executed once with the partial data from the second step.");

            BusEvents.Count.ShouldBe(1);
            var finalFault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            var aggregate = finalFault.InnerException.ShouldBeOfType<AggregateException>();
            var innerFault = aggregate.InnerExceptions.Single().ShouldBeOfType<FaultHubException>();

            innerFault.InnerException.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Nested Step Failed");
            innerFault.AllContexts.ShouldContain("Outer-Tx - Step2EmitsAndFails");
        }    
        
        [Test]
        public async Task RunToEnd_Passes_All_Items_From_Intermediate_Step_To_Next_Step() {
            var source = Observable.Return(0);
            var result = await source.BeginWorkflow("PassAllItems-Tx")
                .Then(_ => Observable.Range(10, 3))
                .Then(items => Observable.Return(items.Sum()))
                .RunToEnd()
                .Capture();
            result.IsCompleted.ShouldBe(true);
            result.Error.ShouldBeNull();
            BusEvents.ShouldBeEmpty();
            result.Items.SelectMany(i => i).Single().ShouldBe(33, "The second step should have received and summed all items from the first step.");
        }

        [Test]
        public async Task RunToEnd_Returns_All_Items_From_Final_Step() {
            var source = Observable.Return(0);
            var result = await source.BeginWorkflow("FinalStep-Tx")
                .Then(_ => Observable.Return("intermediate"))
                .Then(_ => Observable.Range(10, 3))
                .RunToEnd()
                .Capture();
            result.IsCompleted.ShouldBe(true);
            result.Error.ShouldBeNull();
            BusEvents.ShouldBeEmpty();
            result.Items.SelectMany(i=>i).ToArray().ShouldBe([10, 11, 12], "The transaction should return all items from the final step.");
        }

        [Test]
        public async Task Batch_RunToEnd_Returns_All_Items_From_All_Steps() {
            var step1 = Observable.Return("A");
            var step2 = Observable.Range(1, 2).Select(i => i.ToString());
            var step3 = Observable.Return("B");
            var operations = new IObservable<object>[] { step1, step2, step3 };
            var transactionResult = await operations
                .BeginWorkflow("BatchRunToEnd-Tx",TransactionMode.Sequential)
                .RunAndCollect(allItems => Observable.Return(string.Join(",", allItems)))
                .Capture();
            transactionResult.Error.ShouldBeNull();
            transactionResult.IsCompleted.ShouldBe(true);
            BusEvents.ShouldBeEmpty();

            transactionResult.Items.ShouldHaveSingleItem("The transaction should emit a single result from the resultSelector.");
            transactionResult.Items.Single().ShouldBe("A,1,2,B", "The resultSelector should have received all items from all steps in order.");
        }
    
        [Test]
        public async Task BeginWorkflow_Defaults_Transaction_Name_To_Caller_Method_On_FailFast() {
            var source = Observable.Return("start");
            await source.BeginWorkflow()
                .Then(_ => Observable.Throw<string>(new InvalidOperationException("Failure")))
                .RunFailFast()
                .PublishFaults()
                .Capture();
            BusEvents.Count.ShouldBe(1);
            var aborted = BusEvents.Single().ShouldBeOfType<TransactionAbortedException>();
            aborted.Context.BoundaryName.ShouldBe(nameof(BeginWorkflow_Defaults_Transaction_Name_To_Caller_Method_On_FailFast));
        }

        [Test]
        public async Task BeginWorkflow_Defaults_Transaction_Name_To_Caller_Method_On_RunToEnd() {
            var source = Observable.Return("start");
            var stepName = "FailingStep";

            await source.BeginWorkflow()
                .Then(_ => Observable.Throw<string>(new InvalidOperationException("Failure")), stepName: stepName)
                .RunToEnd()
                .PublishFaults()
                .Capture();
            BusEvents.Count.ShouldBe(1);
            var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            fault.Message.ShouldBe($"{nameof(BeginWorkflow_Defaults_Transaction_Name_To_Caller_Method_On_RunToEnd)} completed with errors");

            var innerFault = fault.InnerException.ShouldBeOfType<AggregateException>().InnerExceptions.Single()
                .ShouldBeOfType<FaultHubException>();
            innerFault.AllContexts.ShouldContain($"{nameof(BeginWorkflow_Defaults_Transaction_Name_To_Caller_Method_On_RunToEnd)} - {stepName}");
        }
        
        protected static IEnumerable<TestCaseData> RunSelectors() {
            yield return new TestCaseData(RunFailFast).SetName(nameof(RunFailFast));
            yield return new TestCaseData(RunToEnd).SetName(nameof(RunToEnd)); 
            yield return new TestCaseData(RunAndCollect).SetName(nameof(RunAndCollect));
        }
        private static Func<ITransactionBuilder<string>,IObservable<string[]>> RunFailFast=>builder => builder.RunFailFast();
        private static Func<ITransactionBuilder<string>,IObservable<string[]>> RunToEnd=>builder => builder.RunToEnd();
        private static Func<ITransactionBuilder<string>, IObservable<string[]>> RunAndCollect =>
            builder => builder.Then(stringArray => stringArray.Cast<object>().ToObservable())
                .RunAndCollect(allItems => Observable.Return(allItems.OfType<string>().ToArray()));
        
        [TestCaseSource(nameof(RunSelectors))]
        public async Task Run_Methods_Report_BoundaryName_From_Explicit_Transaction_Name(Func<ITransactionBuilder<string>,IObservable<string[]>> runSelector) {
            var source = Observable.Return("start");
            var explicitName = "MyExplicitTransaction";

            await runSelector(source.BeginWorkflow(explicitName)
                    .Then(_ => Observable.Throw<string>(new InvalidOperationException("Failure"))))
                .PublishFaults()
                .Capture();
            BusEvents.Count.ShouldBe(1);
            var faultHubException = BusEvents.ShouldHaveSingleItem();
            faultHubException.ShouldNotBeNull();
            faultHubException.Message.ShouldContain(explicitName);
            faultHubException.Message.ShouldNotContain(nameof(Run_Methods_Report_BoundaryName_From_Explicit_Transaction_Name));
            if (runSelector.Method.Name.Contains(nameof(RunFailFast))) {
                var abortedException = faultHubException.ShouldBeOfType<TransactionAbortedException>();
                abortedException.Context.BoundaryName.ShouldBe(explicitName);
                abortedException.Context.InnerContext.ShouldNotBeNull();
                abortedException.Context.InnerContext.BoundaryName.ShouldBe("Observable.Throw<string>");
            }
            else {
                faultHubException.Context.BoundaryName.ShouldBe(explicitName);
                faultHubException.Context.InnerContext.ShouldBeNull("RunToEnd/RunAndCollect created a polluted, nested context.");
            }
        }
        
        [TestCaseSource(nameof(RunSelectors))]
        public async Task Run_Methods_Infer_BoundaryName_From_Caller_When_Transaction_Is_Unnamed(Func<ITransactionBuilder<string>,IObservable<string[]>> runSelector) {
            var source = Observable.Return("start");
            
            await runSelector(source.BeginWorkflow()
                    .Then(_ => Observable.Throw<string>(new InvalidOperationException("Failure"))))
                .PublishFaults()
                .Capture();
            BusEvents.Count.ShouldBe(1);
            var faultHubException = BusEvents.ShouldHaveSingleItem();
            faultHubException.ShouldNotBeNull();
            faultHubException.Context.BoundaryName.ShouldBe(nameof(Run_Methods_Infer_BoundaryName_From_Caller_When_Transaction_Is_Unnamed));
            faultHubException.Message.ShouldContain(nameof(Run_Methods_Infer_BoundaryName_From_Caller_When_Transaction_Is_Unnamed));
            var expectedName = nameof(Run_Methods_Infer_BoundaryName_From_Caller_When_Transaction_Is_Unnamed);
            faultHubException.Message.ShouldContain(expectedName);
            
            if (runSelector.Method.Name.Contains(nameof(RunFailFast))) {
                var abortedException = faultHubException.ShouldBeOfType<TransactionAbortedException>();
                abortedException.Context.BoundaryName.ShouldBe(expectedName);
                abortedException.Context.InnerContext.ShouldNotBeNull();
                abortedException.Context.InnerContext.BoundaryName.ShouldBe("Observable.Throw<string>");
            }
            else {
                faultHubException.Context.BoundaryName.ShouldBe(expectedName);
                faultHubException.Context.InnerContext.ShouldBeNull("RunToEnd/RunAndCollect created a polluted, nested context.");
            }
        }
        
        [Test]
        public async Task ConcurrentTransaction_RunToCompletion_Executes_All_And_Aggregates_Failures() {
            var stopwatch = Stopwatch.StartNew();
            var operations = new[] {
                Observable.Timer(100.Milliseconds()).Select(_ => "Success 1"),
                Observable.Timer(150.Milliseconds()).SelectMany(_ => Observable.Throw<string>(new InvalidOperationException("Failure 1"))),
                Observable.Timer(50.Milliseconds()).Select(_ => "Success 2"),
                Observable.Timer(200.Milliseconds()).SelectMany(_ => Observable.Throw<string>(new InvalidOperationException("Failure 2")))
            };
            var result = await operations
                .BeginWorkflow("Concurrent-Tx", mode: TransactionMode.Concurrent)
                .RunToEnd()
                .PublishFaults()
                .Capture();
            stopwatch.Stop();

            stopwatch.ElapsedMilliseconds.ShouldBeLessThan(350);

            result.IsCompleted.ShouldBe(true);

            var successfulResults = result.Items.Single().Cast<string>().ToArray();
            successfulResults.Length.ShouldBe(2);
            successfulResults.ShouldContain("Success 1");
            successfulResults.ShouldContain("Success 2");

            BusEvents.Count.ShouldBe(1);
            var finalFault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            finalFault.AllContexts.ShouldContain("Concurrent-Tx");
            var aggregate = finalFault.InnerException.ShouldBeOfType<FaultHubException>()
                .InnerException.ShouldBeOfType<AggregateException>();
            aggregate.InnerExceptions.Count.ShouldBe(2);

            var failure1 = aggregate.InnerExceptions.OfType<FaultHubException>()
                .FirstOrDefault(ex => ex.InnerException?.Message == "Failure 1");
            failure1.ShouldNotBeNull();
            failure1.AllContexts.ShouldContain($"{nameof(operations)}[1]");

            var failure2 = aggregate.InnerExceptions.OfType<FaultHubException>()
                .FirstOrDefault(ex => ex.InnerException?.Message == "Failure 2");
            failure2.ShouldNotBeNull();
            failure2.AllContexts.ShouldContain($"{nameof(operations)}[3]");
        }

        [Test]
        public async Task ConcurrentTransaction_FailFast_Terminates_On_First_Error() {
            var stopwatch = Stopwatch.StartNew();
            var slowerOperationCompleted = false;

            var concurrentOperations = new[] {
                (Name: "slowOp", Source: Observable.Timer(200.Milliseconds()).Select(_ => (object)"Success - Should be cancelled").Do(_ => slowerOperationCompleted = true)),
                (Name: "fastOp", Source: Observable.Timer(50.Milliseconds()).SelectMany(_ => Observable.Throw<object>(new InvalidOperationException("Fast Failure"))))
            };
            var result = await Observable.Return(Unit.Default)
                .BeginWorkflow("Concurrent-FailFast-Tx")
                .ThenConcurrent(_ => concurrentOperations, failFast: true)
                .RunFailFast()
                .Capture();
            stopwatch.Stop();

            stopwatch.ElapsedMilliseconds.ShouldBeLessThan(150);
            slowerOperationCompleted.ShouldBeFalse("The slower operation should have been cancelled.");

            result.IsCompleted.ShouldBe(false);
            result.Error.ShouldNotBeNull();

            var outerTxException = result.Error.ShouldBeOfType<TransactionAbortedException>();
            outerTxException.Message.ShouldBe("Concurrent-FailFast-Tx failed");
            var innerFault = outerTxException.InnerException.ShouldBeOfType<FaultHubException>();

            innerFault.InnerException.ShouldBeOfType<FaultHubException>().InnerException
                .ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Fast Failure");

            innerFault.AllContexts.ShouldContain(ctx => ctx is string && ((string)ctx).StartsWith("Concurrent-FailFast-Tx -"));

            BusEvents.ShouldBeEmpty();
        }

        [Test]
        public async Task ConcurrentTransaction_Succeeds_When_All_Operations_Succeed() {
            var stopwatch = Stopwatch.StartNew();
            var concurrentOperations = new[] {
                (Name: "Op 1", Source: Observable.Timer(150.Milliseconds()).Select(_ => (object)"Op 1")),
                (Name: "Op 2", Source: Observable.Timer(50.Milliseconds()).Select(_ => (object)"Op 2")),
                (Name: "Op 3", Source: Observable.Timer(100.Milliseconds()).Select(_ => (object)"Op 3"))
            };
            var result = await Observable.Return(Unit.Default)
                .BeginWorkflow("Concurrent-Success-Tx")
                .ThenConcurrent(_ => concurrentOperations)
                .RunToEnd()
                .PublishFaults()
                .Capture();
            stopwatch.Stop();

            stopwatch.ElapsedMilliseconds.ShouldBeInRange(140, 250);

            result.IsCompleted.ShouldBe(true);
            result.Error.ShouldBeNull();

            var successfulResults = result.Items.Single().Single().Cast<string>().ToArray();
            successfulResults.Length.ShouldBe(3);
            Array.Sort(successfulResults);
            successfulResults.ShouldBe(["Op 1", "Op 2", "Op 3"]);

            BusEvents.ShouldBeEmpty();
        }
        [Test]
        public async Task ConcurrentTransaction_Obeys_MaxConcurrency() {
            var stopwatch = Stopwatch.StartNew();
            int activeOperations = 0;
            int maxObservedConcurrency = 0;
            var lockObject = new object();
            var operations = Enumerable.Range(1, 4).Select(i => {
                var obs = Observable.Defer(() => {
                    lock (lockObject) {
                        activeOperations++;
                        maxObservedConcurrency = Math.Max(maxObservedConcurrency, 
                        activeOperations);
                    }
                    return Observable.Timer(150.Milliseconds()).Select(_ => i);
                }).Finally(() => {
                    lock (lockObject) {
                 
                       activeOperations--;
                    }
                });
                return (Name: $"op{i}", Source: obs.Select(val => (object)val));
            });
            var result = await Observable.Return(Unit.Default)
                .BeginWorkflow("Concurrent-Max-Tx")
                .ThenConcurrent(_ => operations, maxConcurrency: 2)
                .RunToEnd()
                .PublishFaults()
                .Capture();
            stopwatch.Stop();

            maxObservedConcurrency.ShouldBe(2);

            stopwatch.ElapsedMilliseconds.ShouldBeInRange(280, 450);

            result.IsCompleted.ShouldBe(true);
            result.Items.Single().Single().Length.ShouldBe(4);
            BusEvents.ShouldBeEmpty();
        }

        [Test]
        public async Task Can_Create_Concurrent_Transaction_From_IEnumerable_Of_Observables() {
            var items = new[] { "Success1", "Failure1", "Success2" };
            var operations = items.Select(item => {
                if (item.StartsWith("Failure")) {
                    return Observable.Throw<string>(new InvalidOperationException(item));
                }
                return Observable.Return(item);
            });
            var result = await operations
                .BeginWorkflow("FromIEnumerableConcurrentTx", mode: TransactionMode.Concurrent)
                .RunToEnd()
                .PublishFaults()
                .Capture();
            result.IsCompleted.ShouldBe(true);
            var successfulResults = result.Items.SelectMany(i => i).Cast<string>().ToArray();
            successfulResults.Length.ShouldBe(2);
            successfulResults.ShouldContain("Success1");
            successfulResults.ShouldContain("Success2");

            BusEvents.Count.ShouldBe(1);
            var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();

            var aggregate = fault.InnerException.ShouldBeOfType<FaultHubException>()
                .InnerException.ShouldBeOfType<AggregateException>();
            var innerFault = aggregate.InnerExceptions.Single().ShouldBeOfType<FaultHubException>();

            innerFault.InnerException.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Failure1");

            innerFault.AllContexts.ShouldContain("operations[1]");
        }
        
        
        [TestCaseSource(nameof(RunSelectors))]
        public async Task Run_Accumulates_Stack_And_Isolates_Context_For_Sequential_Failing_Steps(Func<ITransactionBuilder<string>,IObservable<string[]>> runSelector) {
            IObservable<string> FailingStep1()
                => Observable.Throw<string>(new InvalidOperationException("Failure 1"))
                    .PushStackFrame("Frame_From_Step1");

            IObservable<string> FailingStep2()
                => Observable.Throw<string>(new InvalidOperationException("Failure 2"))
                    .PushStackFrame("Frame_From_Step2");

            var transaction = runSelector(Observable.Return(Unit.Default)
                .BeginWorkflow("IsolationTest")
                .Then(_ => FailingStep1())
                .Then(_ => FailingStep2()));

            await transaction.PublishFaults().Capture();
            BusEvents.Count.ShouldBe(1);

            if (runSelector.Method.Name.Contains(nameof(RunFailFast))) {
                var abortedException = BusEvents.Single().ShouldBeOfType<TransactionAbortedException>();
                var step1Fault = abortedException.InnerException.ShouldBeOfType<FaultHubException>();
                step1Fault.InnerException.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Failure 1");
            }
            else {
                var finalFault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
                var aggregate = finalFault.InnerException.ShouldBeOfType<AggregateException>();
                var step2Fault = aggregate.InnerExceptions.OfType<FaultHubException>()
                    .Single(ex => ex.InnerException?.Message == "Failure 2");

                var step2Stack = step2Fault.LogicalStackTrace.Select(f => f.MemberName).ToArray();

                step2Stack.ShouldNotContain("Frame_From_Step1", 
                    "The stack for Step 2 should be isolated from Step 1 because PushStackFrame cleans up after itself.");
            }
        }

        
        [TestCaseSource(nameof(RunSelectors))]
        public async Task Run_Accumulates_Stack_And_Isolates_Context_For_Failing_Async_Steps(Func<ITransactionBuilder<string>,IObservable<string[]>> runSelector) {
            [MethodImpl(MethodImplOptions.NoInlining)]
            IObservable<string> FailingStep1Async()
                => Observable.Timer(TimeSpan.FromMilliseconds(10))
                    .SelectMany(_ => Observable.Throw<string>(new InvalidOperationException("Failure 1")))
                    .PushStackFrame("Frame_From_Step1");

            [MethodImpl(MethodImplOptions.NoInlining)]
            IObservable<string> FailingStep2Async()
                => Observable.Timer(TimeSpan.FromMilliseconds(10))
                    .SelectMany(_ => Observable.Throw<string>(new InvalidOperationException("Failure 2")))
                    .PushStackFrame("Frame_From_Step2");

            var transaction = runSelector(Observable.Return(Unit.Default)
                .BeginWorkflow("AsyncIsolationTest")
                .Then(_ => FailingStep1Async())
                .Then(_ => FailingStep2Async()));

            await transaction.PublishFaults().Capture();
            BusEvents.Count.ShouldBe(1);

            if (runSelector.Method.Name.Contains(nameof(RunFailFast))) {
                var abortedException = BusEvents.Single().ShouldBeOfType<TransactionAbortedException>();
                var step1Fault = abortedException.InnerException.ShouldBeOfType<FaultHubException>();
                step1Fault.InnerException.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Failure 1");
            }
            else {
                var finalFault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
                var aggregate = finalFault.InnerException.ShouldBeOfType<AggregateException>();
                var step2Fault = aggregate.InnerExceptions.OfType<FaultHubException>()
                    .Single(ex => ex.InnerException?.Message == "Failure 2");

                var step2Stack = step2Fault.LogicalStackTrace.Select(f => f.MemberName).ToArray();

                step2Stack.ShouldNotContain("Frame_From_Step1", 
                    "The stack for Step 2 should be isolated from Step 1 because PushStackFrame cleans up after itself.");
            }        }
        
        [TestCaseSource(nameof(RunSelectors))]
        public async Task Run_Isolates_Stack_For_Nested_Failing_Transaction(Func<ITransactionBuilder<string>,IObservable<string[]>> runSelector) {
            [MethodImpl(MethodImplOptions.NoInlining)]
            IObservable<string> InnerFailingStep()
                => Observable.Throw<string>(new InvalidOperationException("Inner Failure"))
                    .PushStackFrame("Frame_From_Inner_Failure");

            [MethodImpl(MethodImplOptions.NoInlining)]
            IObservable<string[]> NestedFailingTransaction()
                => Observable.Return("")
                    .BeginWorkflow("Nested_Tx")
                    .Then(_ => InnerFailingStep())
                    .RunToEnd();

            [MethodImpl(MethodImplOptions.NoInlining)]
            IObservable<string> OuterFailingStep()
                => Observable.Throw<string>(new InvalidOperationException("Outer Failure"))
                    .PushStackFrame("Frame_From_Outer_Step");

            var transaction = runSelector(Observable.Return("")
                .BeginWorkflow("Outer_Tx")
                .Then(_ => NestedFailingTransaction())
                .Then(_ => OuterFailingStep()));
            
            await transaction.PublishFaults().Capture();

            BusEvents.Count.ShouldBe(1);

            if (runSelector.Method.Name.Contains(nameof(RunFailFast))) {
                var abortedException = BusEvents.Single().ShouldBeOfType<TransactionAbortedException>();

                var nestedStepFault = abortedException.InnerException.ShouldBeOfType<FaultHubException>();
                nestedStepFault.Message.ShouldBe("Nested_Tx completed with errors");
            }
            else {
                var finalFault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
                var aggregate = finalFault.InnerException.ShouldBeOfType<AggregateException>();
                var outerStepFault = aggregate.InnerExceptions.OfType<FaultHubException>()
                    .Single(ex => ex.InnerException?.Message == "Outer Failure");
                
                var outerStepStack = outerStepFault.LogicalStackTrace.Select(f => f.MemberName).ToArray();

                outerStepStack.ShouldNotContain("Frame_From_Inner_Failure", 
                    "The stack for the outer step should be isolated by the preceding nested transaction boundary.");
            }
        }
        
        [Test]
        public async Task NestedRunToEnd_Aggregates_All_Failures_Before_Failing_Outer_FailFast() {
            [MethodImpl(MethodImplOptions.NoInlining)]
            IObservable<Unit> FailingStep2()
                => Observable.Throw<Unit>(new InvalidOperationException("Failure 2"))
                    .PushStackFrame("Frame_From_Step2");

            [MethodImpl(MethodImplOptions.NoInlining)]
            IObservable<Unit> FailingStep1()
                => Observable.Throw<Unit>(new InvalidOperationException("Failure 1"))
                    .PushStackFrame("Frame_From_Step1");
            var nestedTx = Observable.Return(Unit.Default)
                .BeginWorkflow("NestedRunToEnd-Tx")
                .Then(_ => FailingStep1())
                .Then(_ => FailingStep2())
                .RunToEnd();

            await Observable.Return(Unit.Default)
                .BeginWorkflow("OuterFailFast-Tx")
                .Then(_ => nestedTx)
                .RunFailFast()
                .PublishFaults()
                .Capture();

            BusEvents.Count.ShouldBe(1);
            var abortedException = BusEvents.Single().ShouldBeOfType<TransactionAbortedException>();
            abortedException.Context.BoundaryName.ShouldBe("OuterFailFast-Tx");

            var nestedFault = abortedException.InnerException.ShouldBeOfType<FaultHubException>();
            nestedFault.Message.ShouldBe("NestedRunToEnd-Tx completed with errors");

            var aggregate = nestedFault.InnerException.ShouldBeOfType<FaultHubException>()
                .InnerException.ShouldBeOfType<AggregateException>();
            aggregate.InnerExceptions.Count.ShouldBe(2);
            aggregate.InnerExceptions.Any(ex => (ex as FaultHubException)?.InnerException?.Message == "Failure 1").ShouldBeTrue();
            aggregate.InnerExceptions.Any(ex => (ex as FaultHubException)?.InnerException?.Message == "Failure 2").ShouldBeTrue();
        }

        
        [Test]
        public async Task RunToEnd_Continues_After_Nested_TransactionAbortedException() {
            var step2ExecutionCount = 0;

            var innerFault = new FaultHubException("Inner Failure", new InvalidOperationException(), new AmbientFaultContext { BoundaryName = "InnerTx" });
            var failingStep = Observable.Throw<Unit>(new TransactionAbortedException("Aborted", innerFault, new AmbientFaultContext { BoundaryName = "AbortingTx" }));

            IObservable<Unit> SucceedingStep(object[] objects) {
                step2ExecutionCount++;
                return Observable.Return(Unit.Default);
            }

            await new[] { failingStep }
                .BeginWorkflow("TestRunToEnd-Tx", TransactionMode.Sequential)
                .Then(SucceedingStep)
                .RunToEnd()
                .PublishFaults()
                .Capture();

            step2ExecutionCount.ShouldBe(1, "RunToEnd should have continued to the second step after the first failed with TransactionAbortedException.");
        }
        
        [Test]
        public async Task RunToEnd_Aggregates_All_Failures_From_Lazy_IEnumerable_Source() {
            var items = new[] { "Failure1", "Failure2" };
            var operations = items.Select(item => Observable.Throw<string>(new InvalidOperationException(item)));

            await operations
                .BeginWorkflow("PocLazySequenceTx", TransactionMode.Sequential)
                .RunToEnd()
                .PublishFaults()
                .Capture();

            BusEvents.Count.ShouldBe(1);
            var finalFault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            var aggregate = finalFault.InnerException.ShouldBeOfType<AggregateException>();
            
            aggregate.InnerExceptions.Count.ShouldBe(2, "The transaction should have aggregated both failures from the lazy sequence.");
            
            var messages = aggregate.InnerExceptions.OfType<FaultHubException>().Select(f => f.InnerException?.Message).ToHashSet();
            messages.ShouldContain("Failure1");
            messages.ShouldContain("Failure2");
        }
    
        [Test]
        public async Task BeginWorkflow_From_IEnumerable_Creates_Distinct_Steps_For_Sequential_Mode() {
            var step1 = Observable.Throw<Unit>(new InvalidOperationException("Failure 1"));
            var step2 = Observable.Throw<Unit>(new InvalidOperationException("Failure 2"));
            var operations = new[] { step1, step2 };

            await operations
                .BeginWorkflow("MultiStepTx", TransactionMode.Sequential)
                .RunToEnd()
                .PublishFaults()
                .Capture();

            BusEvents.Count.ShouldBe(1);
            var finalFault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            var aggregate = finalFault.InnerException.ShouldBeOfType<AggregateException>();
            aggregate.InnerExceptions.Count.ShouldBe(2);

            var failure1 = aggregate.InnerExceptions.OfType<FaultHubException>()
                .FirstOrDefault(ex => ex.InnerException?.Message == "Failure 1");
            failure1.ShouldNotBeNull();
            failure1.AllContexts.ShouldContain("MultiStepTx - operations[0]", "The first failure did not have the context of the first step.");

            var failure2 = aggregate.InnerExceptions.OfType<FaultHubException>()
                .FirstOrDefault(ex => ex.InnerException?.Message == "Failure 2");
            failure2.ShouldNotBeNull();
            failure2.AllContexts.ShouldContain("MultiStepTx - operations[1]", "The second failure did not have the context of the second step.");
        }
        
        [Test]
        public async Task RunToEnd_Correctly_Buffers_Multi_Emission_From_IEnumerable_Source() {
            var selectorInvocations = 0;
            var source = Observable.Range(1, 3).Select(i => i.ToString());

            var transactionResult = await new[] { source }
                .BeginWorkflow("MultiEmitSource-Tx", TransactionMode.Sequential)
                .RunAndCollect(allItems => {
                    selectorInvocations++;
                    allItems.ShouldBe(["1", "2", "3"]);
                    return Observable.Return(string.Join(",", allItems));
                })
                .Capture();

            transactionResult.Error.ShouldBeNull();
            transactionResult.IsCompleted.ShouldBe(true);
            selectorInvocations.ShouldBe(1, "The result selector should have been invoked only once with all items.");
            transactionResult.Items.ShouldHaveSingleItem();
            transactionResult.Items.Single().ShouldBe("1,2,3");
        }
        
        [Test]
        public async Task RunToEnd_Buffers_Multi_Emission_Step_Before_Passing_To_Nested_Transaction_Step() {
            var nestedTransactionInvocations = 0;
            
            IObservable<Unit[]> NestedTransactionStep(string[] items) {
                nestedTransactionInvocations++;
                items.Length.ShouldBe(3);
                items.ShouldBe(["A", "B", "C"]);

                return new[] { Observable.Return(Unit.Default) }
                    .BeginWorkflow("Nested-Tx-For-Test",null)
                    .RunToEnd();
            }

            var result = await Observable.Return("Start")
                .BeginWorkflow("Parent-Tx")
                .Then(_ => new[] { "A", "B", "C" }.ToObservable())
                .Then(NestedTransactionStep)
                .RunToEnd()
                .Capture();
            
            result.Error.ShouldBeNull();
            result.IsCompleted.ShouldBe(true);
            nestedTransactionInvocations.ShouldBe(1, "The step with the nested transaction should have been invoked only once.");
        }
        
        private int _finalStepInvocationCount;
        private List<int[]> _receivedItemsInFinalStep;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<int> Step2_Emits_Multiple_Items(Unit[] _) 
            => Observable.Range(1, 3);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> FinalStep_Aggregates(int[] items) {
            _finalStepInvocationCount++;
            _receivedItemsInFinalStep.Add(items);
            return Unit.Default.Observe();
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task Then_Step_Buffers_Previous_Multi_Item_Step_Before_Executing(bool failfast) {
            _finalStepInvocationCount = 0;
            _receivedItemsInFinalStep = new List<int[]>();
            
            var result = await Observable.Return(Unit.Default)
                .BeginWorkflow("BufferingTest-Tx")
                .Then(_ => Step2_Emits_Multiple_Items(null))
                .Then(FinalStep_Aggregates).Run(failfast)
                .PublishFaults()
                .Capture();

            result.IsCompleted.ShouldBe(true);
            result.Error.ShouldBeNull();
            BusEvents.ShouldBeEmpty();

            _finalStepInvocationCount.ShouldBe(1, "The final step should have been invoked only once with all items buffered.");
            _receivedItemsInFinalStep.Count.ShouldBe(1);
            _receivedItemsInFinalStep.Single().ShouldBe([1, 2, 3], "The final step did not receive the correctly buffered items.");
        }
        
        [TestCaseSource(nameof(RunSelectors))]
        public async Task Run_Accumulates_Stack_And_Isolates_Context_For_Failing_Steps(Func<ITransactionBuilder<string>,IObservable<string[]>> runSelector) {
            [MethodImpl(MethodImplOptions.NoInlining)]
            IObservable<string> StepAWithInternalStack() =>
                Observable.Throw<string>(new InvalidOperationException("Failure A"))
                    .PushStackFrame("Internal_Frame_A");

            [MethodImpl(MethodImplOptions.NoInlining)]
            IObservable<string> StepBWithInternalStack() =>
                Observable.Throw<string>(new InvalidOperationException("Failure B"))
                    .PushStackFrame("Internal_Frame_B");

            var transaction = runSelector(Observable.Return(Unit.Default)
                .BeginWorkflow("AccumulationTest")
                .Then(_ => StepAWithInternalStack())
                .Then(_ => StepBWithInternalStack()));

            await transaction.PublishFaults().Capture();
            BusEvents.Count.ShouldBe(1);

            if (runSelector.Method.Name.Contains(nameof(RunFailFast))) {
                var abortedException = BusEvents.Single().ShouldBeOfType<TransactionAbortedException>();
                var stepAFault = abortedException.InnerException.ShouldBeOfType<FaultHubException>();
                stepAFault.InnerException.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Failure A");
                
                var stepAStack = stepAFault.LogicalStackTrace.Select(f => f.MemberName).ToArray();
                stepAStack.ShouldContain("Internal_Frame_A");
                stepAStack.ShouldContain(nameof(StepAWithInternalStack));
            }
            else {
                var finalFault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
                var aggregate = finalFault.InnerException.ShouldBeOfType<AggregateException>();
                var stepBFault = aggregate.InnerExceptions.OfType<FaultHubException>()
                    .Single(ex => ex.InnerException?.Message == "Failure B");
                
                var stepBStack = stepBFault.LogicalStackTrace.Select(f => f.MemberName).ToArray();

                stepBStack.ShouldContain(nameof(StepBWithInternalStack));
                stepBStack.ShouldContain("Internal_Frame_B");
                stepBStack.ShouldContain(nameof(StepAWithInternalStack),
                    "The stack for Step B should have accumulated the context from the preceding Step A.");
                Array.IndexOf(stepBStack, nameof(StepBWithInternalStack))
                    .ShouldBeLessThan(Array.IndexOf(stepBStack, nameof(StepAWithInternalStack)),
                        "The frames for Step B should appear before the frames for Step A in the stack.");
            }
            
        }        

        [TestCaseSource(nameof(RunSelectors))]
        public async Task Run_Truncates_Stack_When_Step_Uses_ChainFaultContext(Func<ITransactionBuilder<string>,IObservable<string[]>> runSelector) {
            [MethodImpl(MethodImplOptions.NoInlining)]
            IObservable<string> StepAWithBoundary() =>
                Observable.Throw<string>(new InvalidOperationException("Failure A"))
                    .PushStackFrame("Internal_Frame_A")
                    .ChainFaultContext(["StepA_Context"]);

            [MethodImpl(MethodImplOptions.NoInlining)]
            IObservable<string> StepBWithBoundary() =>
                Observable.Throw<string>(new InvalidOperationException("Failure B"))
                    .PushStackFrame("Internal_Frame_B")
                    .ChainFaultContext(["StepB_Context"]);

            var transaction = runSelector(Observable.Return(Unit.Default)
                .BeginWorkflow("TruncationTest")
                .Then(_ => StepAWithBoundary())
                .Then(_ => StepBWithBoundary()));

            await transaction.PublishFaults().Capture();
            BusEvents.Count.ShouldBe(1);

            if (runSelector.Method.Name.Contains(nameof(RunFailFast))) {
                var abortedException = BusEvents.Single().ShouldBeOfType<TransactionAbortedException>();
                var stepAFault = abortedException.InnerException.ShouldBeOfType<FaultHubException>();
                var businessLogicFault = stepAFault.InnerException.ShouldBeOfType<FaultHubException>();
                var businessLogicStack = businessLogicFault.LogicalStackTrace.Select(f => f.MemberName).ToArray();
                
                businessLogicStack.ShouldContain("Internal_Frame_A");
                businessLogicStack.ShouldNotContain(nameof(StepAWithBoundary), 
                    "The stack should be truncated by the step's ChainFaultContext boundary.");
                stepAFault.AllContexts.ShouldContain("StepA_Context");
            }
            else {
                var finalFault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
                var aggregate = finalFault.InnerException.ShouldBeOfType<AggregateException>();
                var stepBFault = aggregate.InnerExceptions.OfType<FaultHubException>()
                    .Single(ex => ex.InnerException?.Message == "Failure B");
                
                var stepBStack = stepBFault.LogicalStackTrace.Select(f => f.MemberName).ToArray();
                
                stepBStack.ShouldNotContain(nameof(StepAWithBoundary), 
                    "The stack should not accumulate across a ChainFaultContext boundary.");
                stepBFault.AllContexts.ShouldContain("StepB_Context");
                stepBFault.AllContexts.ShouldNotContain("StepA_Context");
            }
        }
        
        [TestCaseSource(nameof(RunSelectors))]
        public async Task Run_Accumulates_Stack_From_Preceding_Successful_Step(Func<ITransactionBuilder<string>,IObservable<string[]>> runSelector) {
            [MethodImpl(MethodImplOptions.NoInlining)]
            IObservable<Unit> StepASucceeds() =>
                Observable.Return(Unit.Default)
                    .PushStackFrame("Frame_From_Successful_Step_A");
            [MethodImpl(MethodImplOptions.NoInlining)]
            IObservable<Unit> StepBFails() =>
                Observable.Throw<Unit>(new InvalidOperationException("Failure B"))
                    .PushStackFrame("Frame_From_Failing_Step_B");
            var transaction = Observable.Return(Unit.Default)
                .BeginWorkflow("TimingAccumulationTest")
                .Then(_ => StepASucceeds())
                .Then(_ => StepBFails())
                .RunToEnd();
            await transaction.PublishFaults().Capture();

            BusEvents.Count.ShouldBe(1);
            var finalFault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            var aggregate = finalFault.InnerException.ShouldBeOfType<AggregateException>();
            var stepBFault = aggregate.InnerExceptions.OfType<FaultHubException>()
                .Single(ex => ex.InnerException?.Message == "Failure B");
            var stepBStack = stepBFault.LogicalStackTrace.Select(f => f.MemberName).ToArray();

            stepBStack.ShouldContain(nameof(StepASucceeds), 
                "The stack for Step B should have accumulated the context from the preceding, successful Step A.");
            
        }
        
        [Test]
        public async Task BeginWorkflow_Pushes_StackFrame_For_Failing_Initial_Source_On_RunFailFast() {
            var source = Observable.Throw<string>(new InvalidOperationException("Initial source failed"));

            var transaction = source.BeginWorkflow()
                                    .RunFailFast();

            await transaction.PublishFaults().Capture();

            BusEvents.Count.ShouldBe(1);
            var abortedException = BusEvents.Single().ShouldBeOfType<TransactionAbortedException>();
            var stepFault = abortedException.InnerException.ShouldBeOfType<FaultHubException>();

            var stepContext = $"{nameof(BeginWorkflow_Pushes_StackFrame_For_Failing_Initial_Source_On_RunFailFast)} - {nameof(source)}";
            stepFault.AllContexts.ShouldContain(stepContext);

            stepFault.LogicalStackTrace.ShouldNotBeEmpty();
            stepFault.LogicalStackTrace.ShouldContain(frame => frame.MemberName == nameof(source));
        }

        [Test]
        public async Task BeginWorkflow_Pushes_StackFrame_For_Failing_Initial_Source_On_RunToEnd() {
            var source = Observable.Throw<string>(new InvalidOperationException("Initial source failed"));

            var transaction = source.BeginWorkflow()
                                    .RunToEnd();

            await transaction.PublishFaults().Capture();

            BusEvents.Count.ShouldBe(1);
            var finalFault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            var aggregate = finalFault.InnerException.ShouldBeOfType<AggregateException>();
            var stepFault = aggregate.InnerExceptions.Single().ShouldBeOfType<FaultHubException>();

            var stepContext = $"{nameof(BeginWorkflow_Pushes_StackFrame_For_Failing_Initial_Source_On_RunToEnd)} - {nameof(source)}";
            stepFault.AllContexts.ShouldContain(stepContext);

            stepFault.LogicalStackTrace.ShouldNotBeEmpty();
            stepFault.LogicalStackTrace.ShouldContain(frame => frame.MemberName == nameof(source));
        }

        [Test]
        public async Task BeginWorkflow_From_IEnumerable_Pushes_StackFrame_For_First_Failing_Source() {
            var operations = new[] {
                Observable.Throw<string>(new InvalidOperationException("First operation failed")),
                Observable.Return("Second operation")
            };

            var transaction = operations.BeginWorkflow(mode: TransactionMode.Sequential)
                                        .RunToEnd();

            await transaction.PublishFaults().Capture();

            BusEvents.Count.ShouldBe(1);
            var finalFault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            var aggregate = finalFault.InnerException.ShouldBeOfType<AggregateException>();
            var stepFault = aggregate.InnerExceptions.Single().ShouldBeOfType<FaultHubException>();
            
            var stepContext = $"{nameof(BeginWorkflow_From_IEnumerable_Pushes_StackFrame_For_First_Failing_Source)} - {nameof(operations)}[0]";
            stepFault.AllContexts.ShouldContain(stepContext);
            
            stepFault.LogicalStackTrace.ShouldNotBeEmpty();
            stepFault.LogicalStackTrace.ShouldContain(frame => frame.MemberName == $"{nameof(operations)}[0]");
        }
        
        [Test]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public async Task BeginWorkflow_Does_Not_Push_Duplicate_Frame() {
            var beginWorkflowDoesNotPushDuplicateFrame = Observable.Throw<Unit>(new InvalidOperationException("Failure")) ;
            var transaction = beginWorkflowDoesNotPushDuplicateFrame.BeginWorkflow()
                .RunFailFast()
                .PushStackFrame();

            await transaction.PublishFaults().Capture();

            BusEvents.Count.ShouldBe(1);
            var abortedException = BusEvents.Single().ShouldBeOfType<TransactionAbortedException>();
            var finalFault = abortedException.InnerException.ShouldBeOfType<FaultHubException>();
            var logicalStack = finalFault.LogicalStackTrace.ToList();


            logicalStack.Count(frame => frame.MemberName==nameof(BeginWorkflow_Does_Not_Push_Duplicate_Frame)).ShouldBe(1);
        }
        
        [Test]
        public async Task RunFailFast_Can_Collect_NonCritical_Errors_And_Fail_At_The_End() {
            var step1 = Observable.Return("Step1");
            var nonCriticalErrorStep = Observable.Throw<string>(new InvalidOperationException("Non-critical failure"));
            var step2 = Observable.Return("Step2");

            await step1
                .BeginWorkflow(nameof(RunFailFast_Can_Collect_NonCritical_Errors_And_Fail_At_The_End))
                .Then(_ => nonCriticalErrorStep, "nonCriticalErrorStep")
                .Then(_ => step2, "step2")
                .RunFailFast(ex => ex is InvalidOperationException)
                .PublishFaults()
                .Capture();

            BusEvents.Count.ShouldBe(1);

            var finalException = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            finalException.Message.ShouldContain(nameof(RunFailFast_Can_Collect_NonCritical_Errors_And_Fail_At_The_End));

            var aggregateException = finalException.InnerException.ShouldBeOfType<AggregateException>();
            
            var stepException = aggregateException.InnerExceptions.Single().ShouldBeOfType<FaultHubException>();
            stepException.InnerException.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Non-critical failure");
            stepException.Context.BoundaryName.ShouldBe("nonCriticalErrorStep");            
        }

        [Test]
        public async Task RunFailFast_Should_FailFast_On_Critical_Error() {
            var step3Executed = false;
            var step1 = Observable.Return("Step1");
            var criticalErrorStep = Observable.Throw<string>(new InvalidOperationException("This is a critical error."));
            var stepThatShouldNotRun = Observable.Defer(() => {
                step3Executed = true;
                return Observable.Return("Step3");
            });

            await step1
                .BeginWorkflow()
                .Then(_ => criticalErrorStep, "CriticalErrorStep")
                .Then(_ => stepThatShouldNotRun, "StepThatShouldNotRun")
                .RunFailFast(ex => ex is TimeoutException)
                .PublishFaults()
                .Capture();

            step3Executed.ShouldBeFalse("The transaction should have aborted before executing the third step.");

            BusEvents.Count.ShouldBe(1);
            var abortedException = BusEvents.Single().ShouldBeOfType<TransactionAbortedException>();
            abortedException.Context.BoundaryName.ShouldBe(nameof(RunFailFast_Should_FailFast_On_Critical_Error));

            var fault = abortedException.InnerException.ShouldBeOfType<FaultHubException>();
            fault.Context.BoundaryName.ShouldBe("CriticalErrorStep");
            fault.InnerException.ShouldBeOfType<InvalidOperationException>()
                .Message.ShouldBe("This is a critical error.");
        }
        
        [Test]
        public async Task Then_With_NonCritical_Predicate_Allows_RunToEnd_To_Continue_And_Aggregate() {
            var step2WasExecuted = false;

            var transaction = Observable.Return("start")
                .BeginWorkflow()
                .Then(
                    _ => Observable.Throw<string>(new InvalidOperationException("This is a non-critical failure.")),
                    isNonCritical: ex => ex is InvalidOperationException
                )
                .Then(results => {
                    step2WasExecuted = true;
                    results.ShouldBeEmpty(); 
                    return SuccessfulOperation(new SubscriptionCounter()).Select(u => (object)u);
                })
                .RunToEnd();

            await transaction.PublishFaults().Capture();

            step2WasExecuted.ShouldBeTrue("The transaction should have continued to the second step.");
            
            BusEvents.Count.ShouldBe(1);
            var finalFault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            var aggregate = finalFault.InnerException.ShouldBeOfType<AggregateException>();
            var stepFault = aggregate.InnerExceptions.Single().ShouldBeOfType<FaultHubException>();
            stepFault.InnerException.ShouldBeOfType<InvalidOperationException>();
            stepFault.Context.Tags.ShouldContain(Transaction.NonCriticalStepTag);
        }
        
        [Test]
        public async Task Then_With_NonCritical_Predicate_Allows_RunFailFast_To_Continue_And_Fail_At_End() {
            var step2WasExecuted = false;

            var transaction = Observable.Return("start")
                .BeginWorkflow()
                .Then(
                    _ => Observable.Throw<string>(new InvalidOperationException("This is a non-critical failure.")),
                    isNonCritical: ex => ex is InvalidOperationException
                )
                .Then(_ => {
                    step2WasExecuted = true;
                    return SuccessfulOperation(new SubscriptionCounter()).Select(u => (object)u);
                })
                .RunFailFast();

            await transaction.PublishFaults().Capture();

            step2WasExecuted.ShouldBeTrue("The transaction should have continued to the second step.");

            BusEvents.Count.ShouldBe(1);
            var finalException = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            finalException.Message.ShouldContain("completed with non-critical errors");
            finalException.Context.Tags.ShouldContain(Transaction.NonCriticalAggregateTag);

            var aggregateException = finalException.InnerException.ShouldBeOfType<AggregateException>();
            var fault = aggregateException.InnerExceptions.Single().ShouldBeOfType<FaultHubException>();
            fault.InnerException.ShouldBeOfType<InvalidOperationException>();
            fault.Context.Tags.ShouldContain(Transaction.NonCriticalStepTag);
        }
        
        [Test]
        public async Task Then_With_NonCritical_Predicate_Overrides_Global_RunFailFast_Predicate() {
            var step2WasExecuted = false;

            var transaction = Observable.Return("start")
                .BeginWorkflow()
                .Then(
                    _ => Observable.Throw<string>(new InvalidOperationException("Step-level non-critical")),
                    isNonCritical: ex => ex is InvalidOperationException 
                )
                .Then(_ => {
                    step2WasExecuted = true;
                    return SuccessfulOperation(new SubscriptionCounter()).Select(u => (object)u);
                })
                .RunFailFast(isNonCritical: ex => ex is TimeoutException); // Global predicate does NOT match

            await transaction.PublishFaults().Capture();

            step2WasExecuted.ShouldBeTrue("Execution should continue because the step-level predicate matched.");

            BusEvents.Count.ShouldBe(1);
            var finalException = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            finalException.Message.ShouldContain("completed with non-critical errors");
        }
        
        [Test]
        public async Task RunFailFast_Aborts_On_Critical_Error_After_Ignoring_NonCritical_Step() {
            var step3WasExecuted = false;

            var transaction = Observable.Return("start")
                .BeginWorkflow()
                .Then(
                    _ => Observable.Throw<string>(new InvalidOperationException("Step-level non-critical")),
                    isNonCritical: ex => ex is InvalidOperationException
                )
                .Then(_ => Observable.Throw<object>(new NotSupportedException("This is critical")))
                .Then(_ => {
                    step3WasExecuted = true;
                    return SuccessfulOperation(new SubscriptionCounter());
                })
                .RunFailFast();

            await transaction.PublishFaults().Capture();

            step3WasExecuted.ShouldBeFalse("Transaction should have aborted on the critical error.");

            BusEvents.Count.ShouldBe(1);
            var abortedException = BusEvents.Single().ShouldBeOfType<TransactionAbortedException>();
            var fault = abortedException.InnerException.ShouldBeOfType<FaultHubException>();
            fault.InnerException.ShouldBeOfType<NotSupportedException>();
            fault.Context.Tags.ShouldNotContain(Transaction.NonCriticalStepTag);
        }
        
        [Test]
        public async Task RunFailFast_Aggregates_Multiple_NonCritical_Errors() {
            var step3WasExecuted = false;

            var transaction = Observable.Return("start")
                .BeginWorkflow()
                .Then(
                    _ => Observable.Throw<string>(new InvalidOperationException("Non-critical 1")),
                    isNonCritical: ex => ex is InvalidOperationException
                )
                .Then(
                    _ => Observable.Throw<object>(new TimeoutException("Non-critical 2")),
                    isNonCritical: ex => ex is TimeoutException
                )
                .Then(_ => {
                    step3WasExecuted = true;
                    return SuccessfulOperation(new SubscriptionCounter());
                })
                .RunFailFast();

            await transaction.PublishFaults().Capture();

            step3WasExecuted.ShouldBeTrue("The transaction should have continued to the final step.");

            BusEvents.Count.ShouldBe(1);
            var finalException = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            finalException.Message.ShouldContain("completed with non-critical errors");

            var aggregate = finalException.InnerException.ShouldBeOfType<AggregateException>();
            aggregate.InnerExceptions.Count.ShouldBe(2);

            aggregate.InnerExceptions.OfType<FaultHubException>()
                .ShouldContain(ex => ex.InnerException is InvalidOperationException);
            aggregate.InnerExceptions.OfType<FaultHubException>()
                .ShouldContain(ex => ex.InnerException is TimeoutException);
        }
    }

    internal class Url { public string Href { get; set; } }
}
