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
            
            finalFault.LogicalStackTrace.ShouldContain(frame => frame.Context.Contains("MyTransaction"));
            
            var aggregateException = finalFault.InnerException.ShouldBeOfType<AggregateException>();
            var innerFault = aggregateException.InnerExceptions.Single().ShouldBeOfType<FaultHubException>();

            innerFault.AllContexts.ShouldNotContain("MyTransaction");
            innerFault.AllContexts.ShouldContain("MyTransaction - Op:1");
        }
        [Test]
        public async Task SequentialTransaction_Aborts_On_First_Failure_When_FailFast_Is_True() {
            var operations = new object[] {
                Observable.Return(Unit.Default),
                Observable.Throw<string>(new InvalidOperationException("Failing Operation")),
                Observable.Return(Unit.Default)
            };

            await operations.SequentialTransaction(true, transactionName:"FailFastTransaction")
                .PublishFaults().Capture();

            var finalFault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            
            var txException = finalFault.InnerException.ShouldBeOfType<InvalidOperationException>();
            txException.Message.ShouldBe("FailFastTransaction failed");

            var originalFault = txException.InnerException.ShouldBeOfType<FaultHubException>();
            originalFault.InnerException.ShouldBeOfType<InvalidOperationException>().Message
                .ShouldBe("Failing Operation");
        }

        [Test]
        public async Task NestedFailFastTransaction_Aborts_Outer_FailFastTransaction() {
            var innerTx1 = new object[] { Observable.Return(Unit.Default), Observable.Throw<Unit>(new InvalidOperationException("Inner Tx1 Failed")) }
                .SequentialTransaction(true, transactionName:"InnerTransaction1");

            var innerTx2 = new object[] { Observable.Return(Unit.Default) }
                .SequentialTransaction(transactionName:"InnerTransaction2");
            
            await new object[] { innerTx1, innerTx2 }
                .SequentialTransaction(true, transactionName:"OuterTransaction")
                .PublishFaults().Capture();
            
            var finalFault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            
            var outerTxException = finalFault.InnerException.ShouldBeOfType<InvalidOperationException>();
            outerTxException.Message.ShouldBe("OuterTransaction failed");
            
            var innerFault = outerTxException.InnerException.ShouldBeOfType<FaultHubException>();
            var innerTxException = innerFault.InnerException.ShouldBeOfType<InvalidOperationException>();
            innerTxException.Message.ShouldBe("InnerTransaction1 failed");
            
            var originalOpFault = innerTxException.InnerException.ShouldBeOfType<FaultHubException>();
            originalOpFault.InnerException.ShouldBeOfType<InvalidOperationException>().Message
                .ShouldBe("Inner Tx1 Failed");
        }

        [Test]
        public async Task NestedRunToCompletionTransaction_Fails_OuterFailFastTransaction_After_Completion() {
            var innerTx1 = new object[] { Unit.Default.Observe() }
                .SequentialTransaction(true, transactionName:"InnerTransaction1");

            var innerTx2 = new object[] { Unit.Default.Observe(), Observable.Throw<Unit>(new InvalidOperationException("Inner Tx2 Failed")) }
                .SequentialTransaction(transactionName:"InnerTransaction2");
            
            await new object[] { innerTx1, innerTx2 }
                .SequentialTransaction(true, transactionName:"OuterTransaction")
                .PublishFaults().Capture();

            var finalFault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            
            var outerTxException = finalFault.InnerException.ShouldBeOfType<InvalidOperationException>();
            outerTxException.Message.ShouldBe("OuterTransaction failed");
            
            var innerFault = outerTxException.InnerException.ShouldBeOfType<FaultHubException>();
            var aggregateException = innerFault.InnerException.ShouldBeOfType<AggregateException>();
            var originalOpFault = aggregateException.InnerExceptions.Single().ShouldBeOfType<FaultHubException>();
            
            originalOpFault.InnerException?.Message.ShouldBe("Inner Tx2 Failed");
        }

        [Test]
        public async Task RunToCompletionOuterTransaction_Aggregates_Failures_From_Nested_Transactions() {
            var innerTx1 = new object[] { Observable.Throw<Unit>(new InvalidOperationException("Inner Tx1 Failed")) }
                .SequentialTransaction(true, transactionName:"InnerTransaction1");
            
            var innerTx2 = new object[] { Observable.Throw<Unit>(new InvalidOperationException("Inner Tx2 Failed")) }
                .SequentialTransaction(transactionName:"InnerTransaction2");
            
            await new object[] { innerTx1, innerTx2 }
                .SequentialTransaction(transactionName:"OuterTransaction")
                .PublishFaults().Capture();
            
            var finalFault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            
            var aggregateException = finalFault.InnerException.ShouldBeOfType<AggregateException>();
            aggregateException.InnerExceptions.Count.ShouldBe(2);
            
            var failure1 = aggregateException.InnerExceptions.OfType<FaultHubException>()
                .FirstOrDefault(ex => ex.AllContexts.Contains("OuterTransaction - Op:1"));
            failure1.ShouldNotBeNull();
            
            var innerException1 = failure1.InnerException.ShouldBeOfType<InvalidOperationException>();
            innerException1.Message.ShouldBe("InnerTransaction1 failed");
            
            var failure2 = aggregateException.InnerExceptions.OfType<FaultHubException>()
                .FirstOrDefault(ex => ex.AllContexts.Contains("OuterTransaction - Op:2"));
            failure2.ShouldNotBeNull();

            var innerAggregate2 = failure2.InnerException.ShouldBeOfType<AggregateException>();
            innerAggregate2.InnerExceptions.Single().ShouldBeOfType<FaultHubException>();
        }
    }
}