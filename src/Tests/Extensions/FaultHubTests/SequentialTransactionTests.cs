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
    public class SequentialTransactionTests : FaultHubTestBase {
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
        public async Task SequentialTransaction_Aborts_On_First_Failure_When_FailFast_Is_True() {
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
                .SequentialTransaction(true, op => op.Retry(2), transactionName: "FailFastTransaction")
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
            }.SequentialTransaction(true, op => op, transactionName: "InnerTransaction1");

            var innerTx2 = new object[] {
                Unit.Default.Observe().TrackSubscriptions(innerTx2SuccessOpCounter)
            }.SequentialTransaction(false, op => op, transactionName: "InnerTransaction2");

            var outerOperations = new object[] { innerTx1, innerTx2 };

            await outerOperations
                .SequentialTransaction(true, op => op, transactionName: "OuterTransaction")
                .PublishFaults().Capture();

            BusEvents.Count.ShouldBe(1);

            innerTx1SuccessOpCounter.Count.ShouldBe(1);
            innerTx1FailingOpCounter.Count.ShouldBe(1);
            innerTx1SkippedOpCounter.Count.ShouldBe(0);

            innerTx2SuccessOpCounter.Count.ShouldBe(0);

            var finalFault = BusEvents.Single().ShouldBeOfType<FaultHubException>();

            var outerTxException = finalFault.InnerException.ShouldBeOfType<InvalidOperationException>();
            outerTxException.Message.ShouldBe("OuterTransaction failed");
            finalFault.AllContexts.ShouldContain("OuterTransaction");

            var innerTxFault = outerTxException.InnerException.ShouldBeOfType<FaultHubException>();
            var innerTxException = innerTxFault.InnerException.ShouldBeOfType<InvalidOperationException>();
            innerTxException.Message.ShouldBe("InnerTransaction1 failed");
            innerTxFault.AllContexts.ShouldContain("InnerTransaction1");

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
            }.SequentialTransaction(true, op => op, transactionName: "InnerTransaction1");

            var innerTx2 = new object[] {
                Unit.Default.Observe().TrackSubscriptions(innerTx2SuccessOp1Counter),
                Observable.Throw<Unit>(new InvalidOperationException("Inner Tx2 Failed"))
                    .TrackSubscriptions(innerTx2FailingOpCounter),
                Unit.Default.Observe().TrackSubscriptions(innerTx2SuccessOp2Counter)
            }.SequentialTransaction(false, op => op, transactionName: "InnerTransaction2");

            var outerOperations = new object[] { innerTx1, innerTx2 };

            await outerOperations
                .SequentialTransaction(true, op => op, transactionName: "OuterTransaction")
                .PublishFaults().Capture();

            BusEvents.Count.ShouldBe(1);

            innerTx1SuccessOpCounter.Count.ShouldBe(1);

            innerTx2SuccessOp1Counter.Count.ShouldBe(1);
            innerTx2FailingOpCounter.Count.ShouldBe(1);
            innerTx2SuccessOp2Counter.Count.ShouldBe(1);

            var finalFault = BusEvents.Single().ShouldBeOfType<FaultHubException>();

            var outerTxException = finalFault.InnerException.ShouldBeOfType<InvalidOperationException>();
            outerTxException.Message.ShouldBe("OuterTransaction failed");
            finalFault.AllContexts.ShouldContain("OuterTransaction");

            var innerTxFault = outerTxException.InnerException.ShouldBeOfType<FaultHubException>();
            var innerTxException = innerTxFault.InnerException.ShouldBeOfType<InvalidOperationException>();
            innerTxException.Message.ShouldBe("InnerTransaction2 failed");
            innerTxFault.AllContexts.ShouldContain("InnerTransaction2");

            var originalOperationalFault = innerTxException.InnerException.ShouldBeOfType<AggregateException>()
                .InnerExceptions.Single().ShouldBeOfType<FaultHubException>();

            originalOperationalFault.AllContexts.ShouldContain("InnerTransaction2 - Op:2");
            originalOperationalFault.InnerException?.Message.ShouldBe("Inner Tx2 Failed");
        }

        [Test]
        public async Task RunToCompletionOuterTransaction_Aggregates_Failures_From_Nested_Transactions() {
            var innerTx1SuccessOpCounter = new SubscriptionCounter();
            var innerTx1FailingOpCounter = new SubscriptionCounter();
            var innerTx1SkippedOpCounter = new SubscriptionCounter();

            var innerTx2SuccessOp1Counter = new SubscriptionCounter();
            var innerTx2FailingOpCounter = new SubscriptionCounter();
            var innerTx2SuccessOp2Counter = new SubscriptionCounter();

            var innerTx1 = new object[] {
                Unit.Default.Observe().TrackSubscriptions(innerTx1SuccessOpCounter),
                Observable.Throw<Unit>(new InvalidOperationException("Inner Tx1 Failed"))
                    .TrackSubscriptions(innerTx1FailingOpCounter),
                Unit.Default.Observe().TrackSubscriptions(innerTx1SkippedOpCounter)
            }.SequentialTransaction(true, op => op, transactionName: "InnerTransaction1");

            var innerTx2 = new object[] {
                Unit.Default.Observe().TrackSubscriptions(innerTx2SuccessOp1Counter),
                Observable.Throw<Unit>(new InvalidOperationException("Inner Tx2 Failed"))
                    .TrackSubscriptions(innerTx2FailingOpCounter),
                Unit.Default.Observe().TrackSubscriptions(innerTx2SuccessOp2Counter)
            }.SequentialTransaction(false, op => op, transactionName: "InnerTransaction2");

            var outerOperations = new object[] { innerTx1, innerTx2 };

            await outerOperations
                .SequentialTransaction(false, op => op, transactionName: "OuterTransaction")
                .PublishFaults().Capture();

            BusEvents.Count.ShouldBe(1);

            innerTx1SuccessOpCounter.Count.ShouldBe(1);
            innerTx1FailingOpCounter.Count.ShouldBe(1);
            innerTx1SkippedOpCounter.Count.ShouldBe(0);

            innerTx2SuccessOp1Counter.Count.ShouldBe(1);
            innerTx2FailingOpCounter.Count.ShouldBe(1);
            innerTx2SuccessOp2Counter.Count.ShouldBe(1);

            var finalFault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            var outerTxException = finalFault.InnerException.ShouldBeOfType<InvalidOperationException>();
            outerTxException.Message.ShouldBe("OuterTransaction failed");

            var aggregateException = outerTxException.InnerException.ShouldBeOfType<AggregateException>();
            aggregateException.InnerExceptions.Count.ShouldBe(2);

            var failure1 = aggregateException.InnerExceptions.OfType<FaultHubException>()
                .FirstOrDefault(ex => ex.AllContexts.Contains("OuterTransaction - Op:1"));
            failure1.ShouldNotBeNull();

            var innerTxException1 = failure1.InnerException.ShouldBeOfType<InvalidOperationException>();
            innerTxException1.Message.ShouldBe("InnerTransaction1 failed");
            failure1.AllContexts.ShouldContain("InnerTransaction1");

            var originalOperationalFault1 = innerTxException1.InnerException.ShouldBeOfType<FaultHubException>();
            originalOperationalFault1.InnerException.ShouldBeOfType<InvalidOperationException>().Message
                .ShouldBe("Inner Tx1 Failed");
            originalOperationalFault1.AllContexts.ShouldContain("InnerTransaction1 - Op:2");

            var failure2 = aggregateException.InnerExceptions.OfType<FaultHubException>()
                .FirstOrDefault(ex => ex.AllContexts.Contains("OuterTransaction - Op:2"));
            failure2.ShouldNotBeNull();

            var innerTxException2 = failure2.InnerException.ShouldBeOfType<InvalidOperationException>();
            innerTxException2.Message.ShouldBe("InnerTransaction2 failed");
            failure2.AllContexts.ShouldContain("InnerTransaction2");

            var originalOperationalFault2 = innerTxException2.InnerException.ShouldBeOfType<AggregateException>()
                .InnerExceptions.Single().ShouldBeOfType<FaultHubException>();
            originalOperationalFault2.InnerException.ShouldBeOfType<InvalidOperationException>().Message
                .ShouldBe("Inner Tx2 Failed");
            originalOperationalFault2.AllContexts.ShouldContain("InnerTransaction2 - Op:2");
        }
    }
}