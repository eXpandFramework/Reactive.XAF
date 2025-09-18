using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.Relay;
using Xpand.Extensions.Reactive.Relay.Transaction;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Tests.FaultHubTests.TransactionalApi {
    public class TransactionalApiTaggingTests  : FaultHubTestBase {
        [Test]
        public async Task RunToEnd_Adds_Sequential_And_RunToEnd_Tags() {
            var transaction = Observable.Throw<Unit>(new InvalidOperationException("test"))
                .BeginWorkflow("MyTestTx")
                .RunToEnd();
            await transaction.PublishFaults().Capture();

            var finalFault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            finalFault.Context.Tags.ShouldContain(Transaction.TransactionNodeTag);
            finalFault.Context.Tags.ShouldContain(nameof(TransactionMode.Sequential));
            finalFault.Context.Tags.ShouldContain(nameof(Transaction.RunToEnd));
        }

        [Test]
        public async Task RunFailFast_Adds_RunFailFast_Tag_To_AbortedException() {
            var transaction = Observable.Throw<Unit>(new InvalidOperationException("test"))
                .BeginWorkflow("MyFailFastTx")
                .RunFailFast();
            await transaction.PublishFaults().Capture();

            var abortedException = BusEvents.Single().ShouldBeOfType<TransactionAbortedException>();
            abortedException.Context.Tags.ShouldContain(Transaction.TransactionNodeTag);
            abortedException.Context.Tags.ShouldContain(nameof(Transaction.RunFailFast));
            abortedException.Context.Tags.ShouldContain(nameof(TransactionMode.Sequential));
        }

        [Test]
        public async Task BeginWorkflow_Adds_Concurrent_Tag_For_Concurrent_Mode() {
            var operations = new[] { Observable.Throw<Unit>(new InvalidOperationException("test")) };
            var transaction = operations
                .BeginWorkflow("MyConcurrentTx", TransactionMode.Concurrent)
                .RunToEnd();
            await transaction.PublishFaults().Capture();

            var finalFault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            finalFault.Context.Tags.ShouldContain(Transaction.TransactionNodeTag);
            finalFault.Context.Tags.ShouldContain(nameof(TransactionMode.Concurrent));
        }

        [Test]
        public async Task RunAndCollect_Adds_RunAndCollect_Tag() {
            var transaction = Observable.Throw<object>(new InvalidOperationException("test"))
                .BeginWorkflow("MyCollectTx")
                .RunAndCollect(_ => Observable.Return(Unit.Default));
            await transaction.PublishFaults().Capture();

            var finalFault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            finalFault.Context.Tags.ShouldContain(Transaction.TransactionNodeTag);
            finalFault.Context.Tags.ShouldContain(nameof(Transaction.RunAndCollect));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> Failing_Step_For_Tag_Test()
            => Observable.Throw<Unit>(new InvalidOperationException("Step Failure"));

        [Test]
        public async Task BeginWorkflow_Adds_Transaction_Tag() {
            var transaction = Observable.Throw<Unit>(new InvalidOperationException("test"))
                .BeginWorkflow("MyTestTx")
                .RunToEnd();
            await transaction.PublishFaults().Capture();

            var finalFault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            finalFault.Context.Tags.ShouldContain(Transaction.TransactionNodeTag,
                "The top-level transaction's context was not tagged correctly.");
        }


        [Test]
        public async Task RunToEnd_Adds_Step_Tag() {
            var transaction = Observable.Return(Unit.Default)
                .BeginWorkflow("OuterTx")
                .Then(_ => Failing_Step_For_Tag_Test())
                .RunToEnd();
            await transaction.PublishFaults().Capture();

            var finalFault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            var aggregate = finalFault.InnerException.ShouldBeOfType<AggregateException>();
            var stepFault = aggregate.InnerExceptions.Single().ShouldBeOfType<FaultHubException>();
            stepFault.Context.Tags.ShouldContain(Transaction.StepNodeTag, "The failing step's context was not tagged correctly.");
        }
        
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit[]> InnerFailingNestedTransaction()
            => Observable.Throw<Unit>(new InvalidOperationException("Inner Failure"))
                .BeginWorkflow("InnerTx")
                .RunToEnd();

        [Test]
        public async Task Nested_Transaction_Is_Tagged_As_Nested() {
            var transaction = Observable.Return(Unit.Default)
                .BeginWorkflow("OuterTx")
                .Then(_ => InnerFailingNestedTransaction())
                .RunToEnd();

            await transaction.PublishFaults().Capture();
            var finalFault = BusEvents.Single().ShouldBeOfType<FaultHubException>();

            var aggregate = finalFault.InnerException.ShouldBeOfType<AggregateException>();
            var stepFault = aggregate.InnerExceptions.Single().ShouldBeOfType<FaultHubException>();

            var innerTxContext = stepFault.Context.InnerContext;
            innerTxContext.ShouldNotBeNull("The context from the inner transaction was not preserved.");

            innerTxContext.Tags.ShouldContain(Transaction.TransactionNodeTag);
            innerTxContext.Tags.ShouldContain(Transaction.NestedTransactionNodeTag);
        }
    }
}