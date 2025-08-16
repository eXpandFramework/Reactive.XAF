using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Tests.FaultHubTests {
    public class ExecuteTransactionTests : FaultHubTestBase {
        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<string> FailingOperationWithContext(SubscriptionCounter failingCounter)
            => Observable.Throw<string>(new InvalidOperationException("Operation Failed"))
                .TrackSubscriptions(failingCounter)
                .PushStackFrame();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> SuccessfulOperationWithContext(SubscriptionCounter successfulCounter)
            => Unit.Default.Observe()
                .TrackSubscriptions(successfulCounter)
                .PushStackFrame();

        [Test]
        public async Task Inner_Operation_Failure_Triggers_Outer_Transactional_Retry() {
            var failingCounter = new SubscriptionCounter();
            var successfulCounter = new SubscriptionCounter();
            var outerCounter = new SubscriptionCounter();

            var result = await new object[] {
                    FailingOperationWithContext(failingCounter),
                    SuccessfulOperationWithContext(successfulCounter)
                }
                .ExecuteTransaction(op => op.Retry(2), ["MyCustomContext"], false, "MyTransaction")
                .TrackSubscriptions(outerCounter)
                .ChainFaultContext(s => s.Retry(3), ["Outer"])
                .PublishFaults().Capture();

            result.IsCompleted.ShouldBe(true);
            result.Error.ShouldBeNull();

            BusEvents.Count.ShouldBe(1);
            (failingCounter.Count, successfulCounter.Count, outerCounter.Count).ShouldBe((6, 3, 3));

            var finalFault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            finalFault.AllContexts.ShouldContain("Outer");

            var transactionException = finalFault.InnerException.ShouldBeOfType<InvalidOperationException>();
            transactionException.Message.ShouldBe("MyTransaction failed");

            var innerFault = transactionException.InnerException.ShouldBeOfType<AggregateException>()
                .InnerExceptions.Single().ShouldBeOfType<FaultHubException>();
            innerFault.AllContexts.ShouldContain("MyTransaction - Op:1");
            innerFault.AllContexts.ShouldContain("MyCustomContext");
            innerFault.InnerException.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Operation Failed");

            var logicalStack = innerFault.LogicalStackTrace.ToArray();
            logicalStack.ShouldNotBeEmpty();
            logicalStack.First().MemberName.ShouldBe(nameof(FailingOperationWithContext));
        }

        [Test]
        public async Task ExecuteTransaction_Aborts_On_First_Failure_When_FailFast_Is_True() {
            var successfulOp1Counter = new SubscriptionCounter();
            var failingOpCounter = new SubscriptionCounter();
            var successfulOp2Counter = new SubscriptionCounter();

            var operations = new object[] {
                Unit.Default.Observe().TrackSubscriptions(successfulOp1Counter),
                Observable.Throw<string>(new InvalidOperationException("Failing Operation"))
                    .TrackSubscriptions(failingOpCounter),
                Unit.Default.Observe().TrackSubscriptions(successfulOp2Counter)
            };

            var result = await operations
                .ExecuteTransaction(op => op.Retry(2), true, "FailFastTransaction")
                .PublishFaults().Capture();

            result.IsCompleted.ShouldBe(true);
            result.Error.ShouldBeNull();

            BusEvents.Count.ShouldBe(1);

            successfulOp1Counter.Count.ShouldBe(1);
            failingOpCounter.Count.ShouldBe(2);
            successfulOp2Counter.Count.ShouldBe(0);

            var finalFault = BusEvents.Single().ShouldBeOfType<FaultHubException>();

            var txException = finalFault.InnerException.ShouldBeOfType<InvalidOperationException>();
            txException.Message.ShouldBe("FailFastTransaction failed");

            var originalFault = txException.InnerException.ShouldBeOfType<FaultHubException>();
            originalFault.InnerException.ShouldBeOfType<InvalidOperationException>().Message
                .ShouldBe("Failing Operation");
            originalFault.AllContexts.ShouldContain("FailFastTransaction - Op:2");
        }

        [Test]
        public async Task NestedFailFastTransaction_Aborts_OuterFailFastTransaction() {
            var innerTx1SuccessOpCounter = new SubscriptionCounter();
            var innerTx1FailingOpCounter = new SubscriptionCounter();
            var innerTx1SkippedOpCounter = new SubscriptionCounter();
            var innerTx2SuccessOpCounter = new SubscriptionCounter();

            var innerTx1 = new object[] {
                Unit.Default.Observe().TrackSubscriptions(innerTx1SuccessOpCounter),
                Observable.Throw<Unit>(new InvalidOperationException("Inner Tx1 Failed"))
                    .TrackSubscriptions(innerTx1FailingOpCounter),
                Unit.Default.Observe().TrackSubscriptions(innerTx1SkippedOpCounter)
            }.ExecuteTransaction(op => op, true, "InnerTransaction1");

            var innerTx2 = new object[] {
                Unit.Default.Observe().TrackSubscriptions(innerTx2SuccessOpCounter)
            }.ExecuteTransaction(op => op, false, "InnerTransaction2");

            var outerOperations = new object[] { innerTx1, innerTx2 };

            //Act
            await outerOperations
                .ExecuteTransaction(op => op, true, "OuterTransaction")
                .PublishFaults().Capture();

            //Assert
            BusEvents.Count.ShouldBe(1);

            // Inner Transaction 1 (FailFast) asserts
            innerTx1SuccessOpCounter.Count.ShouldBe(1); // The first op runs.
            innerTx1FailingOpCounter.Count.ShouldBe(1); // The second op fails.
            innerTx1SkippedOpCounter.Count.ShouldBe(0); // The third op is skipped.

            // Inner Transaction 2 asserts
            innerTx2SuccessOpCounter.Count.ShouldBe(0); // The entire second transaction is skipped.

            var finalFault = BusEvents.Single().ShouldBeOfType<FaultHubException>();

            // 1. The Story of the Outer Transaction's Failure
            var outerTxException = finalFault.InnerException.ShouldBeOfType<InvalidOperationException>();
            outerTxException.Message.ShouldBe("OuterTransaction failed");
            finalFault.AllContexts.ShouldContain("OuterTransaction");

            // 2. The Story of the Inner Transaction's Failure (the cause of the outer failure)
            var innerTxFault = outerTxException.InnerException.ShouldBeOfType<FaultHubException>();
            var innerTxException = innerTxFault.InnerException.ShouldBeOfType<InvalidOperationException>();
            innerTxException.Message.ShouldBe("InnerTransaction1 failed");
            innerTxFault.AllContexts.ShouldContain("InnerTransaction1");

            // 3. The Story of the Original Operational Failure (the root cause)
            var originalOperationalFault = innerTxException.InnerException.ShouldBeOfType<FaultHubException>();
            originalOperationalFault.InnerException.ShouldBeOfType<InvalidOperationException>().Message
                .ShouldBe("Inner Tx1 Failed");
            originalOperationalFault.AllContexts.ShouldContain("InnerTransaction1 - Op:2");
        }

        [Test]
        public async Task NestedRunToCompletionTransaction_Fails_OuterFailFastTransaction_After_Completion() {
            var innerTx1SuccessOpCounter = new SubscriptionCounter();
            var innerTx2SuccessOp1Counter = new SubscriptionCounter();
            var innerTx2FailingOpCounter = new SubscriptionCounter();
            var innerTx2SuccessOp2Counter = new SubscriptionCounter();

            var innerTx1 = new object[] {
                Unit.Default.Observe().TrackSubscriptions(innerTx1SuccessOpCounter)
            }.ExecuteTransaction(op => op, true, "InnerTransaction1");

            var innerTx2 = new object[] {
                Unit.Default.Observe().TrackSubscriptions(innerTx2SuccessOp1Counter),
                Observable.Throw<Unit>(new InvalidOperationException("Inner Tx2 Failed"))
                    .TrackSubscriptions(innerTx2FailingOpCounter),
                Unit.Default.Observe().TrackSubscriptions(innerTx2SuccessOp2Counter)
            }.ExecuteTransaction(op => op, false, "InnerTransaction2");

            var outerOperations = new object[] { innerTx1, innerTx2 };

            //Act
            await outerOperations
                .ExecuteTransaction(op => op, true, "OuterTransaction")
                .PublishFaults().Capture();

            BusEvents.Count.ShouldBe(1);

            innerTx1SuccessOpCounter.Count.ShouldBe(1); // Runs and succeeds.

            innerTx2SuccessOp1Counter.Count.ShouldBe(1);
            innerTx2FailingOpCounter.Count.ShouldBe(1);
            innerTx2SuccessOp2Counter.Count.ShouldBe(1);

            var finalFault = BusEvents.Single().ShouldBeOfType<FaultHubException>();

            // 1. The Story of the Outer Transaction's Failure
            var outerTxException = finalFault.InnerException.ShouldBeOfType<InvalidOperationException>();
            outerTxException.Message.ShouldBe("OuterTransaction failed");
            finalFault.AllContexts.ShouldContain("OuterTransaction");

            // 2. The Story of the Inner Transaction's Failure (the cause of the outer failure)
            var innerTxFault = outerTxException.InnerException.ShouldBeOfType<FaultHubException>();
            var innerTxException = innerTxFault.InnerException.ShouldBeOfType<InvalidOperationException>();
            innerTxException.Message.ShouldBe("InnerTransaction2 failed");
            innerTxFault.AllContexts.ShouldContain("InnerTransaction2");

            // 3. The Story of the Original Operational Failure (the root cause)
            var originalOperationalFault = innerTxException.InnerException.ShouldBeOfType<AggregateException>()
                .InnerExceptions.Single().ShouldBeOfType<FaultHubException>();

            originalOperationalFault.AllContexts.ShouldContain("InnerTransaction2 - Op:2");
            originalOperationalFault.InnerException?.Message.ShouldBe("Inner Tx2 Failed");
        }

        [Test]
        public async Task RunToCompletionOuterTransaction_Aggregates_Failures_From_Nested_Transactions() {
            //Arrange
            var innerTx1SuccessOpCounter = new SubscriptionCounter();
            var innerTx1FailingOpCounter = new SubscriptionCounter();
            var innerTx1SkippedOpCounter = new SubscriptionCounter();

            var innerTx2SuccessOp1Counter = new SubscriptionCounter();
            var innerTx2FailingOpCounter = new SubscriptionCounter();
            var innerTx2SuccessOp2Counter = new SubscriptionCounter();

            // This inner transaction will fail fast.
            var innerTx1 = new object[] {
                Unit.Default.Observe().TrackSubscriptions(innerTx1SuccessOpCounter),
                Observable.Throw<Unit>(new InvalidOperationException("Inner Tx1 Failed"))
                    .TrackSubscriptions(innerTx1FailingOpCounter),
                Unit.Default.Observe().TrackSubscriptions(innerTx1SkippedOpCounter)
            }.ExecuteTransaction(op => op, true, "InnerTransaction1");

            // This inner transaction will run to completion.
            var innerTx2 = new object[] {
                Unit.Default.Observe().TrackSubscriptions(innerTx2SuccessOp1Counter),
                Observable.Throw<Unit>(new InvalidOperationException("Inner Tx2 Failed"))
                    .TrackSubscriptions(innerTx2FailingOpCounter),
                Unit.Default.Observe().TrackSubscriptions(innerTx2SuccessOp2Counter)
            }.ExecuteTransaction(op => op, false, "InnerTransaction2");

            var outerOperations = new object[] { innerTx1, innerTx2 };

            //Act
            // The outer transaction is NOT failFast.
            await outerOperations
                .ExecuteTransaction(op => op, false, "OuterTransaction")
                .PublishFaults().Capture();

            //Assert
            BusEvents.Count.ShouldBe(1);

            // Inner Transaction 1 (FailFast) asserts
            innerTx1SuccessOpCounter.Count.ShouldBe(1); // The first op runs.
            innerTx1FailingOpCounter.Count.ShouldBe(1); // The second op fails.
            innerTx1SkippedOpCounter.Count.ShouldBe(0); // The third op is skipped.

            // Inner Transaction 2 (Run-to-Completion) asserts
            innerTx2SuccessOp1Counter.Count.ShouldBe(1); // The first op runs.
            innerTx2FailingOpCounter.Count.ShouldBe(1); // The second op fails.
            innerTx2SuccessOp2Counter.Count.ShouldBe(1); // The third op ALSO runs.

            var finalFault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            var outerTxException = finalFault.InnerException.ShouldBeOfType<InvalidOperationException>();
            outerTxException.Message.ShouldBe("OuterTransaction failed");

            // The outer transaction should have an AggregateException containing two distinct failures.
            var aggregateException = outerTxException.InnerException.ShouldBeOfType<AggregateException>();
            aggregateException.InnerExceptions.Count.ShouldBe(2);

            // Verify the failure from the first inner transaction
            var failure1 = aggregateException.InnerExceptions.OfType<FaultHubException>()
                .FirstOrDefault(ex => ex.AllContexts.Contains("OuterTransaction - Op:1"));
            failure1.ShouldNotBeNull();

            // 1. The Story of the Inner Transaction's Failure
            var innerTxException1 = failure1.InnerException.ShouldBeOfType<InvalidOperationException>();
            innerTxException1.Message.ShouldBe("InnerTransaction1 failed");
            failure1.AllContexts.ShouldContain("InnerTransaction1");

            // 2. The Story of the Original Operational Failure (the root cause)
            var originalOperationalFault1 = innerTxException1.InnerException.ShouldBeOfType<FaultHubException>();
            originalOperationalFault1.InnerException.ShouldBeOfType<InvalidOperationException>().Message
                .ShouldBe("Inner Tx1 Failed");
            originalOperationalFault1.AllContexts.ShouldContain("InnerTransaction1 - Op:2");

            // Verify the failure from the second inner transaction
            var failure2 = aggregateException.InnerExceptions.OfType<FaultHubException>()
                .FirstOrDefault(ex => ex.AllContexts.Contains("OuterTransaction - Op:2"));
            failure2.ShouldNotBeNull();

            // 1. The Story of the Inner Transaction's Failure
            var innerTxException2 = failure2.InnerException.ShouldBeOfType<InvalidOperationException>();
            innerTxException2.Message.ShouldBe("InnerTransaction2 failed");
            failure2.AllContexts.ShouldContain("InnerTransaction2");

            // 2. The Story of the Original Operational Failure (the root cause)
            var originalOperationalFault2 = innerTxException2.InnerException.ShouldBeOfType<AggregateException>()
                .InnerExceptions.Single().ShouldBeOfType<FaultHubException>();
            originalOperationalFault2.InnerException.ShouldBeOfType<InvalidOperationException>().Message
                .ShouldBe("Inner Tx2 Failed");
            originalOperationalFault2.AllContexts.ShouldContain("InnerTransaction2 - Op:2");
        }
    }
}