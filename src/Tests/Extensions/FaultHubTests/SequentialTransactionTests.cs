using System;
using System.Collections.Generic;
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
        
        [Test]
        public async Task Two_Part_Transaction_Waits_For_First_Stream_And_Propagates_Results() {
            var executionLog = new List<string>();
            var source = Observable.Range(1, 3)
                .Do(i => executionLog.Add($"Part 1 Emitted: {i}"))
                .DoOnComplete(() => executionLog.Add("Part 1 Finalized"));

            IObservable<int> SecondStreamSelector(IList<int> results) {
                executionLog.Add("Part 2 Started");
                results.ShouldBe([1, 2, 3]);
                return Observable.Return(results.Sum());
            }

            var result = await source
                .SequentialTransaction((Func<IList<int>, IObservable<int>>)SecondStreamSelector, transactionName: "TwoPartTx")
                .Capture();

            result.IsCompleted.ShouldBeTrue();
            result.Error.ShouldBeNull();
            result.Items.Single().ShouldBe(6);

            executionLog.ShouldBe(new[] {
                "Part 1 Emitted: 1",
                "Part 1 Emitted: 2",
                "Part 1 Emitted: 3",
                "Part 1 Finalized",
                "Part 2 Started"
            });
            BusEvents.ShouldBeEmpty();
        }

        [Test]
        public async Task Two_Part_Transaction_Fails_If_First_Part_Fails() {
            var secondPartStarted = false;
            var source = Observable.Range(1, 2)
                .Concat(Observable.Throw<int>(new InvalidOperationException("Failure in Part 1")));

            IObservable<Unit> SecondStreamSelector(IList<int> _) {
                secondPartStarted = true;
                return Observable.Empty<Unit>();
            }

            var result = await source
                .SequentialTransaction((Func<IList<int>, IObservable<Unit>>)SecondStreamSelector, transactionName: "TwoPartTxFailure1")
                .PublishFaults()
                .Capture();

            result.IsCompleted.ShouldBeTrue();
            result.Error.ShouldBeNull();
            secondPartStarted.ShouldBeFalse();

            BusEvents.Count.ShouldBe(1);
            var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            fault.InnerException.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Failure in Part 1");

            fault.AllContexts.ShouldContain("TwoPartTxFailure1");
            fault.LogicalStackTrace.ShouldContain(frame
                => frame.Context.Last() as string == "TwoPartTxFailure1 - Part 1");
        }

        [Test]
        public async Task Two_Part_Transaction_Fails_If_Second_Part_Fails() {
            var source = Observable.Range(1, 3);

            IObservable<int> SecondStreamSelector(IList<int> results) {
                results.ShouldBe([1, 2, 3]);
                return Observable.Throw<int>(new InvalidOperationException("Failure in Part 2"));
            }

            var result = await source
                .SequentialTransaction((Func<IList<int>, IObservable<int>>)SecondStreamSelector, transactionName: "TwoPartTxFailure2")
                .PublishFaults()
                .Capture();

            result.IsCompleted.ShouldBeTrue();
            result.Error.ShouldBeNull();

            BusEvents.Count.ShouldBe(1);
            var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            fault.InnerException.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Failure in Part 2");

            fault.AllContexts.ShouldContain("TwoPartTxFailure2");
            fault.LogicalStackTrace.ShouldContain(frame => frame.Context.First() as string == "TwoPartTxFailure2 - Part 2");
        }
        
        [Test]
        public async Task Two_Part_Transaction_RunToCompletion_Executes_Second_Part_When_First_Fails() {
            var secondPartStarted = false;
            var source = Observable.Throw<int>(new InvalidOperationException("Part 1 Always Fails"));

            IObservable<int> SecondStreamSelector(IList<int> results) {
                secondPartStarted = true;
                results.ShouldBeEmpty();
                return Observable.Return(999);
            }

            var result = await source
                .SequentialTransaction((Func<IList<int>, IObservable<int>>)SecondStreamSelector, failFast: false, transactionName: "RunToCompletionTx")
                .PublishFaults()
                .Capture();

            result.IsCompleted.ShouldBeTrue();
            result.Error.ShouldBeNull();
            result.Items.Single().ShouldBe(999);
            secondPartStarted.ShouldBeTrue();

            BusEvents.Count.ShouldBe(1);
            var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            fault.InnerException.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Part 1 Always Fails");

            fault.AllContexts.ShouldContain("RunToCompletionTx");
            fault.LogicalStackTrace.ShouldContain(frame => frame.Context.Last() as string == "RunToCompletionTx - Part 1");
        }
        
        
    }
}