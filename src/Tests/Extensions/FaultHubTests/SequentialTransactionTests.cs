using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Tests.FaultHubTests {
    public class SequentialTransactionTests : FaultHubTestBase {
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

            var result = await FailingOperation(failingCounter) 
                .BeginBatchTransaction()
                .Add(SuccessfulOperation(successfulCounter))
                .SequentialTransaction(false, op => op.Retry(2), ["MyCustomContext"], "MyTransaction")
                .TrackSubscriptions(outerCounter)
                .ChainFaultContext(s => s.Retry(3), ["Outer"])
                .PublishFaults().Capture();

            result.IsCompleted.ShouldBe(true);
            result.Error.ShouldBeNull();
            BusEvents.Count.ShouldBe(1);
            (failingCounter.Count, successfulCounter.Count, outerCounter.Count).ShouldBe((6, 3, 3));

            var finalFault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            finalFault.AllContexts.ShouldContain("Outer");
            
            finalFault.LogicalStackTrace.ShouldContain(frame => frame.Context.Contains("MyTransaction"));
            
            var aggregateException = finalFault.InnerException.ShouldBeOfType<AggregateException>();
            var innerFault = aggregateException.InnerExceptions.Single().ShouldBeOfType<FaultHubException>();

            innerFault.AllContexts.ShouldNotContain("MyTransaction");
            innerFault.AllContexts.ShouldContain("MyTransaction - FailingOperation(failingCounter)");
        }

        [Test]
        public async Task SequentialTransaction_Aborts_On_First_Failure_When_FailFast_Is_True() {
            await Observable.Return(Unit.Default)
                .BeginBatchTransaction()
                .Add(Observable.Throw<string>(new InvalidOperationException("Failing Operation")))
                .Add(Observable.Return(Unit.Default))
                .SequentialTransaction(true, transactionName: "FailFastTransaction")
                .PublishFaults().Capture();

// MODIFICATION: The assertions are updated to expect the new TransactionAbortedException,
// which correctly wraps the underlying FaultHubException without the extra InvalidOperationException.
            var finalFault = BusEvents.Single().ShouldBeOfType<TransactionAbortedException>();
            finalFault.Message.ShouldBe("FailFastTransaction failed");

            var originalFault = finalFault.InnerException.ShouldBeOfType<FaultHubException>();
            originalFault.InnerException.ShouldBeOfType<InvalidOperationException>().Message
                .ShouldBe("Failing Operation");
        }

        [Test]
        public async Task Can_Create_Transaction_From_IEnumerable_Of_Observables() {
            var result = await new[] { "Success1", "Failure1", "Success2" }
                .Select(item => item.StartsWith("Failure") ? Observable.Throw<string>(new InvalidOperationException(item)) : Observable.Return(item))
                .BeginBatchTransaction()
                .SequentialTransaction(transactionName: "FromIEnumerableTx")
                .PublishFaults()
                .Capture();

            result.IsCompleted.ShouldBe(true);
            result.Items.ShouldBeEmpty();

            BusEvents.Count.ShouldBe(1);
            var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
    
            var aggregate = fault.InnerException.ShouldBeOfType<AggregateException>();
            var innerFault = aggregate.InnerExceptions.Single().ShouldBeOfType<FaultHubException>();

            innerFault.InnerException.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Failure1");

            var contextString = innerFault.AllContexts.OfType<string>()
                .FirstOrDefault(s => s.StartsWith("FromIEnumerableTx -") && s.EndsWith("[1]"));

            contextString.ShouldNotBeNull();

        }
        
        [Test]
        public async Task NestedFailFastTransaction_Aborts_Outer_FailFastTransaction() {
            var innerTx1 = Observable.Return(Unit.Default)
                .BeginBatchTransaction()
                .Add(Observable.Throw<Unit>(new InvalidOperationException("Inner Tx1 Failed")))
                .SequentialTransaction(true, transactionName: "InnerTransaction1");

            var innerTx2 = Observable.Return(Unit.Default)
                .BeginBatchTransaction()
                .SequentialTransaction(transactionName:"InnerTransaction2");

            await innerTx1
                .BeginBatchTransaction()
                .Add(innerTx2)
                .SequentialTransaction(true, transactionName:"OuterTransaction")
                .PublishFaults().Capture();
            
            var finalFault = BusEvents.Single().ShouldBeOfType<TransactionAbortedException>();
            finalFault.Message.ShouldBe("OuterTransaction failed");

            var innerFault = finalFault.InnerException.ShouldBeOfType<FaultHubException>();
            innerFault.Message.ShouldBe("InnerTransaction1 failed");

            var originalOpFault = innerFault.InnerException.ShouldBeOfType<FaultHubException>();
            originalOpFault.InnerException.ShouldBeOfType<InvalidOperationException>().Message
                .ShouldBe("Inner Tx1 Failed");

            finalFault.AllContexts.ShouldContain("OuterTransaction - innerTx1");
            originalOpFault.AllContexts.ShouldContain("InnerTransaction1 - Observable.Throw<Unit>(new InvalidOperationException(\"Inner Tx1 Failed\"))");
        }

        [Test]
        public async Task NestedRunToCompletionTransaction_Fails_OuterFailFastTransaction_After_Completion() {
            var innerTx1 = Observable.Return(Unit.Default)
                .BeginBatchTransaction()
                .SequentialTransaction(true, transactionName:"InnerTransaction1");

            var innerTx2 = Observable.Return(Unit.Default)
                .BeginBatchTransaction()
                .Add(Observable.Throw<Unit>(new InvalidOperationException("Inner Tx2 Failed")))
                .SequentialTransaction(transactionName:"InnerTransaction2");

            await innerTx1
                .BeginBatchTransaction()
                .Add(innerTx2)
                .SequentialTransaction(true, transactionName:"OuterTransaction")
                .PublishFaults().Capture();

            var finalFault = BusEvents.Single().ShouldBeOfType<TransactionAbortedException>();
            finalFault.Message.ShouldBe("OuterTransaction failed");

            var innerFault = finalFault.InnerException.ShouldBeOfType<FaultHubException>();
            innerFault.Message.ShouldBe("InnerTransaction2 completed with errors");

            var aggregateException = innerFault.InnerException.ShouldBeOfType<AggregateException>();
            var originalOpFault = aggregateException.InnerExceptions.Single().ShouldBeOfType<FaultHubException>();

            originalOpFault.InnerException?.Message.ShouldBe("Inner Tx2 Failed");

            finalFault.AllContexts.ShouldContain("OuterTransaction - innerTx2");
            originalOpFault.AllContexts.ShouldContain("InnerTransaction2 - Observable.Throw<Unit>(new InvalidOperationException(\"Inner Tx2 Failed\"))");
        }

        [Test]
        public async Task RunToCompletionOuterTransaction_Aggregates_Failures_From_Nested_Transactions() {
            var innerTx1 = Observable.Throw<Unit>(new InvalidOperationException("Inner Tx1 Failed"))
                .BeginBatchTransaction()
                .SequentialTransaction(true, transactionName: "InnerTransaction1");

            var innerTx2 = Observable.Throw<Unit>(new InvalidOperationException("Inner Tx2 Failed"))
                .BeginBatchTransaction()
                .SequentialTransaction(transactionName: "InnerTransaction2");

            await innerTx1
                .BeginBatchTransaction()
                .Add(innerTx2)
                .SequentialTransaction(transactionName: "OuterTransaction")
                .PublishFaults().Capture();

            var finalFault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            finalFault.Message.ShouldBe("OuterTransaction completed with errors");

            var aggregateException = finalFault.InnerException.ShouldBeOfType<AggregateException>();
            aggregateException.InnerExceptions.Count.ShouldBe(2);

            var failure1 = aggregateException.InnerExceptions.OfType<TransactionAbortedException>().Single();
            failure1.Message.ShouldBe("InnerTransaction1 failed");
            failure1.InnerException.ShouldBeOfType<FaultHubException>()
                .InnerException.ShouldBeOfType<InvalidOperationException>()
                .Message.ShouldBe("Inner Tx1 Failed");

            var failure2 = aggregateException.InnerExceptions.OfType<FaultHubException>()
                .Single(ex => ex is not TransactionAbortedException);
            failure2.Message.ShouldBe("InnerTransaction2 completed with errors");
            failure2.InnerException.ShouldBeOfType<AggregateException>()
                .InnerExceptions.Single().ShouldBeOfType<FaultHubException>()
                .InnerException.ShouldBeOfType<InvalidOperationException>()
                .Message.ShouldBe("Inner Tx2 Failed");
        }
        public class Customer { public int Id { get; set; } }
        public class Order { public int Id { get; set; } }


        [Test]
        public async Task Fluent_Builder_Four_Part_Transaction_Fails_On_Last_Part() {
            var source = Observable.Return(123);

            IObservable<Customer> Step2GetCustomer(int[] ids) => Observable.Return(new Customer { Id = ids.Single() });
            IObservable<List<Order>> Step3GetOrders(Customer[] c) => Observable.Return(new List<Order> { new() { Id = 1 } });
            IObservable<Unit> Step4ProcessOrders(List<Order>[] o) => Observable.Throw<Unit>(new InvalidOperationException("Order processing failed"));

            var result = await source.BeginWorkflow("Four-Part-Tx")
                .Then(Step2GetCustomer)
                .Then(Step3GetOrders)
                .Then(Step4ProcessOrders)
                .RunFailFast()
                .PublishFaults()
                .Capture();

            result.IsCompleted.ShouldBe(true);
            BusEvents.Count.ShouldBe(1);
            var finalFault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            finalFault.InnerException.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Order processing failed");
            var logicalStack = finalFault.LogicalStackTrace.ToArray();
            logicalStack.Single().MemberName.ShouldBe("FailFast");

            finalFault.AllContexts.ShouldContain("Four-Part-Tx - Step4ProcessOrders");

            finalFault.AllContexts.ShouldContain("Four-Part-Tx");
        }        
        
        [Test]
        public async Task Fluent_Builder_RunToCompletion_Executes_All_Parts_And_Aggregates_Failures() {
            var part1Counter = 0;
            var part2Counter = 0;
            var part3Counter = 0;
            var part4Counter = 0;
            var source = Observable.Return(123)
                .Do(_ => part1Counter++);

            IObservable<Customer> Step2GetCustomer(int[] ints) {
                part2Counter++;
                return Observable.Throw<Customer>(new InvalidOperationException("Customer lookup failed"));
            }

            IObservable<List<Order>> Step3GetOrders(Customer[] customer) {
                part3Counter++;
                customer.ShouldBeEmpty();
                return Observable.Return(new List<Order>());
            }

            IObservable<Unit> Step4ProcessOrders(List<Order>[] _) {
                part4Counter++;
                return Observable.Throw<Unit>(new InvalidOperationException("Order processing failed"));
            }

            await source.BeginWorkflow("RunToCompletion-Tx")
                .Then(Step2GetCustomer)
                .Then(Step3GetOrders)
                .Then(Step4ProcessOrders)
                .RunToEnd()
                .PublishFaults()
                .Capture();

            (part1Counter, part2Counter, part3Counter, part4Counter).ShouldBe((1, 1, 1, 1));

            BusEvents.Count.ShouldBe(1);
            var finalFault = BusEvents.Single().ShouldBeOfType<FaultHubException>();

            var aggregate = finalFault.InnerException.ShouldBeOfType<AggregateException>();
            aggregate.InnerExceptions.Count.ShouldBe(2);

            var fault2 = aggregate.InnerExceptions.OfType<FaultHubException>()
                .FirstOrDefault(ex => ex.AllContexts.Contains("RunToCompletion-Tx - Step2GetCustomer"));
            fault2.ShouldNotBeNull();
            fault2.InnerException.ShouldBeOfType<InvalidOperationException>().Message
                .ShouldBe("Customer lookup failed");

            var fault4 = aggregate.InnerExceptions.OfType<FaultHubException>()
                .FirstOrDefault(ex => ex.AllContexts.Contains("RunToCompletion-Tx - Step4ProcessOrders"));
            fault4.ShouldNotBeNull();
            fault4.InnerException.ShouldBeOfType<InvalidOperationException>().Message
                .ShouldBe("Order processing failed");
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

            IObservable<Unit> SecondStreamSelector(int[] _) {
                secondPartStarted = true;
                return Observable.Empty<Unit>();
            }

            await source
                .BeginWorkflow("TwoPartTxFailure1")
                .Then(SecondStreamSelector)
                .RunFailFast()
                .PublishFaults()
                .Capture();

            secondPartStarted.ShouldBeFalse();

            var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            fault.InnerException.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Failure in Part 1");

            var logicalStack = fault.LogicalStackTrace.ToArray();
            logicalStack.Length.ShouldBe(1);
            logicalStack.Single().MemberName.ShouldBe("FailFast");

            fault.AllContexts.ShouldContain("TwoPartTxFailure1 - source");
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
            innerFault.AllContexts.ShouldContain("SyncException-Tx - First");
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

            innerFault.AllContexts.ShouldContain("RunToCompletionTx - source");
        }    
        [Test]
        public async Task Fluent_Builder_Uses_Fallback_On_Failure_And_Continues_Transaction() {
            var primarySelectorCalled = false;
            var fallbackSelectorCalled = false;
            var finalStepReceivedCorrectData = false;

            var source = Observable.Return(123);

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
        }        
        [Test]
        public async Task Fluent_Builder_FailFast_Ignores_Fallback_And_Propagates_Error() {
            var primarySelectorCalled = false;
            var fallbackSelectorCalled = false;
            
            var source = Observable.Return(123);

            IObservable<string> FailingStep(int[] ints) {
                primarySelectorCalled = true;
                return Observable.Throw<string>(new InvalidOperationException("Primary step failed"));
            }

            IObservable<string> FallbackStep(Exception ex, int[] ints) {
                fallbackSelectorCalled = true;
                return Observable.Return("Fallback Data");
            }

            await source.BeginWorkflow("FailFast-Fallback-Tx")
                .Then(FailingStep, fallbackSelector:FallbackStep)
                .RunFailFast()
                .PublishFaults()
                .Capture();

            primarySelectorCalled.ShouldBe(true);
            fallbackSelectorCalled.ShouldBe(false);

            BusEvents.Count.ShouldBe(1);
            var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            fault.InnerException.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Primary step failed");
            
            var logicalStack = fault.LogicalStackTrace.ToArray();
            logicalStack.Length.ShouldBe(1);
            var stepFrame = logicalStack.Single();
            fault.AllContexts.ShouldContain("FailFast-Fallback-Tx - FailingStep");
            stepFrame.MemberName.ShouldBe("FailFast");
        }

        [Test]
        public async Task RunToEnd_TopLevel_Transaction_Fails_Atomically_And_Discards_Partial_Results() {
            var source = Observable.Return("Initial Value");
            IObservable<string> Step2Succeeds(string[] _) => Observable.Return("Step 2 Succeeded");

            IObservable<string> Step3Fails(string[] _)
                => Observable.Throw<string>(new InvalidOperationException("Step 3 Failed"));

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

            var logicalStack = innerFault.LogicalStackTrace.ToArray();
            logicalStack.Length.ShouldBe(1);
            logicalStack.Single().MemberName.ShouldBe("RunToEnd");
        }

        [TestCase]
        public async Task Nested_RunToEnd_Feeds_Next_Step_With_Partial_Results_Before_Propagating_Failure() {
            var step1Succeeded = false;
            var finalStepWasCalled = false;
            var dataReceivedInFinalStep = new List<string>();

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
            result.Error.ShouldBeOfType<FaultHubException>();

            var topLevelAggregate = result.Error.InnerException.ShouldBeOfType<AggregateException>();
            var originalFailure = topLevelAggregate.InnerExceptions.Single().ShouldBeOfType<FaultHubException>();
            var aggregate = originalFailure.InnerException.ShouldBeOfType<AggregateException>();
            var innerFault = aggregate.InnerExceptions.Single().ShouldBeOfType<FaultHubException>();
            innerFault.InnerException.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Inner Failure");

            var expectedContext = "Outer-Tx - _ => InnerFailingTransaction()";
            var actualContext = originalFailure.AllContexts.OfType<string>()
                .FirstOrDefault(s => s.StartsWith("Outer-Tx -"));
            actualContext.ShouldNotBeNull();
            var normalizedExpected = System.Text.RegularExpressions.Regex.Replace(expectedContext, @"\s", "");
            var normalizedActual = System.Text.RegularExpressions.Regex.Replace(actualContext, @"\s", "");
            normalizedActual.ShouldBe(normalizedExpected);

            innerFault.AllContexts.ShouldContain("Nested-Tx - InnerStep2Fails");
            innerFault.AllContexts.ShouldNotContain("Nested-Tx - InnerStep1Succeeds");

            var logicalStack = innerFault.LogicalStackTrace.ToArray();
            logicalStack.Length.ShouldBe(1);
            logicalStack.Single().MemberName.ShouldBe("RunToEnd");
        }
        public static IEnumerable<TestCaseData> Lambda_Naming_Cases() {
            Combine.ITransactionBuilder<Unit> ImplicitNameAction(Combine.ITransactionBuilder<string> builder) 
                => builder.Then(_ => Observable.Throw<Unit>(new InvalidOperationException("Lambda Failed")));
            var expectedImplicitName = "_ => Observable.Throw<Unit>(new InvalidOperationException(\"Lambda Failed\"))";
            yield return new TestCaseData((Func<Combine.ITransactionBuilder<string>, Combine.ITransactionBuilder<Unit>>)ImplicitNameAction, expectedImplicitName).SetName("Implicit Lambda Name");

            Combine.ITransactionBuilder<Unit> ExplicitNameAction(Combine.ITransactionBuilder<string> builder)
                => builder.Then(_ => Observable.Throw<Unit>(new InvalidOperationException("Lambda Failed")), "MyExplicitLambdaName");
            var expectedExplicitName = "MyExplicitLambdaName";
            yield return new TestCaseData((Func<Combine.ITransactionBuilder<string>, Combine.ITransactionBuilder<Unit>>)ExplicitNameAction, expectedExplicitName).SetName("Explicit Lambda Name");
        }

        [Test]
        [TestCaseSource(nameof(Lambda_Naming_Cases))]
        public async Task Fluent_Builder_Correctly_Names_Failing_Lambda_Step(
            Func<Combine.ITransactionBuilder<string>, Combine.ITransactionBuilder<Unit>> configureTransaction,
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
        }    }
}