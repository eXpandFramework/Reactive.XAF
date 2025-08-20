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
        
        public class Customer { public int Id { get; set; } }
        public class Order { public int Id { get; set; } }


        [Test]
        public async Task Fluent_Builder_Four_Part_Transaction_Fails_On_Last_Part() {
            var source = Observable.Return(123);
            IObservable<Customer> Step2GetCustomer(IList<int> ids) => Observable.Return(new Customer { Id = ids.Single() });
            IObservable<List<Order>> Step3GetOrders(Customer c) => Observable.Return(new List<Order> { new() { Id = 1 } });
            IObservable<Unit> Step4ProcessOrders(List<Order> o) => Observable.Throw<Unit>(new InvalidOperationException("Order processing failed"));

            var result = await source.BeginTransaction("Four-Part-Tx")
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
            logicalStack.Length.ShouldBe(4);
            ((string)logicalStack[0].Context.Last()).ShouldBe("Four-Part-Tx - Part 4");
            ((string)logicalStack[3].Context.Last()).ShouldBe("Four-Part-Tx - Part 1");
        }
        
        [Test]
        public async Task Fluent_Builder_RunToCompletion_Executes_All_Parts_And_Aggregates_Failures() {
            var part1Counter = 0;
            var part2Counter = 0;
            var part3Counter = 0;
            var part4Counter = 0;

            var source = Observable.Return(123)
                .Do(_ => part1Counter++);

            IObservable<Customer> Step2GetCustomer(IList<int> _) {
                part2Counter++;
                return Observable.Throw<Customer>(new InvalidOperationException("Customer lookup failed"));
            }

            IObservable<List<Order>> Step3GetOrders(Customer customer) {
                part3Counter++;
                customer.ShouldBeNull();
                return Observable.Return(new List<Order>());
            }

            IObservable<Unit> Step4ProcessOrders(List<Order> _) {
                part4Counter++;
                return Observable.Throw<Unit>(new InvalidOperationException("Order processing failed"));
            }

            await source.BeginTransaction("RunToCompletion-Tx")
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
                .FirstOrDefault(ex
                    => ex.LogicalStackTrace.Any(f => (string)f.Context.Last() == "RunToCompletion-Tx - Part 2"));
            fault2.ShouldNotBeNull();
            fault2.InnerException.ShouldBeOfType<InvalidOperationException>().Message
                .ShouldBe("Customer lookup failed");

            var fault4 = aggregate.InnerExceptions.OfType<FaultHubException>()
                .FirstOrDefault(ex
                    => ex.LogicalStackTrace.Any(f => (string)f.Context.Last() == "RunToCompletion-Tx - Part 4"));
            fault4.ShouldNotBeNull();
            fault4.InnerException.ShouldBeOfType<InvalidOperationException>().Message
                .ShouldBe("Order processing failed");
        }
        
        [Test]
        public async Task Fluent_Builder_Succeeds() {
            var source = Observable.Range(1, 3);
            IObservable<int> SecondStreamSelector(IList<int> results) => Observable.Return(results.Sum());
            var result = await source
                .BeginTransaction("TwoPartTx")
                .Then(SecondStreamSelector)
                .RunFailFast()
                .Capture();

            result.Items.Single().ShouldBe(6);
            BusEvents.ShouldBeEmpty();
        }

        [Test]
        public async Task Fluent_Builder_Fails_If_First_Part_Fails() {
            var secondPartStarted = false;
            var source = Observable.Throw<int>(new InvalidOperationException("Failure in Part 1"));

            IObservable<Unit> SecondStreamSelector(IList<int> _) {
                secondPartStarted = true;
                return Observable.Empty<Unit>();
            }

            await source
                .BeginTransaction("TwoPartTxFailure1")
                .Then(SecondStreamSelector)
                .RunFailFast()
                .PublishFaults()
                .Capture();

            secondPartStarted.ShouldBeFalse();
            var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            fault.InnerException.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Failure in Part 1");
            fault.LogicalStackTrace.Single().Context.Last().ShouldBe("TwoPartTxFailure1 - Part 1");
        }


        [Test]
        public async Task Fluent_Builder_RunToCompletion_When_First_Part_Fails() {
            var part1Counter = 0;
            var part2Counter = 0;
            var source = Observable.Throw<int>(new InvalidOperationException("Part 1 Always Fails"))
                .Do(_ => part1Counter++, _ => part1Counter++);

            IObservable<int> SecondStreamSelector(IList<int> results) {
                part2Counter++;
                results.ShouldBeNull();
                return Observable.Return(999);
            }

            var result = await source
                .BeginTransaction("RunToCompletionTx")
                .Then(SecondStreamSelector)
                .RunToEnd()
                .PublishFaults()
                .Capture();

            (part1Counter, part2Counter).ShouldBe((1, 1));
            result.IsCompleted.ShouldBe(true);
            result.Items.ShouldBeEmpty();

            var finalFault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            var aggregate = finalFault.InnerException.ShouldBeOfType<AggregateException>();
            var innerFault = aggregate.InnerExceptions.Single().ShouldBeOfType<FaultHubException>();

            innerFault.InnerException.ShouldBeOfType<InvalidOperationException>().Message
                .ShouldBe("Part 1 Always Fails");
            innerFault.LogicalStackTrace.Single().Context.Last().ShouldBe("RunToCompletionTx - Part 1");
        }
        
    }
}