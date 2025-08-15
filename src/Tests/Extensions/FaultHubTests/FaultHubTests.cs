using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.ExceptionExtensions;
using Xpand.Extensions.Numeric;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.ErrorHandling;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Tests.FaultHubTests{
    public class FaultHubTests : FaultHubTestBase {
        [Test]
        public async Task Exceptions_emitted_from_FaultHub() {
            var stream = Observable.Throw<Unit>(new Exception()).ContinueOnFault().PublishFaults();
            var result = await stream.Capture();

            result.Error.ShouldBeNull();
            BusEvents.Count.ShouldBe(1);
        }
        
        [Test]
        public async Task Resilient_streams_complete_and_do_not_throw() {
            var stream = Observable.Throw<Unit>(new Exception()).ContinueOnFault();
            var result = await stream.Take(1).PublishFaults().Capture();
            result.Error.ShouldBeNull();
            result.IsCompleted.ShouldBeTrue();
            BusEvents.Count.ShouldBe(1);
        }

        [Test]
        public async Task Resilient_streams_do_not_throw_when_inner_stream_throws() {
            var stream = Observable.Defer(() => Observable.Defer(() => Observable.Throw<Unit>(new Exception())))
                .ContinueOnFault();

            var result = await stream.Take(1).PublishFaults().Capture();

            result.Error.ShouldBeNull();
            result.IsCompleted.ShouldBeTrue();
            BusEvents.Count.ShouldBe(1);
        }

        [Test]
        public async Task Any_UpStream_emits_until_completion_when_inner_resilient_stream_throws() {
            int count = 0;
            var stream = 1.Range(3).ToObservable().Do(_ => count++)
                .SelectMany(_ => Observable.Defer(() => Observable.Throw<Unit>(new Exception())).ContinueOnFault());

            var result = await stream.PublishFaults().Capture();

            result.ItemCount.ShouldBe(0);
            result.Error.ShouldBeNull();
            result.IsCompleted.ShouldBeTrue();
            count.ShouldBe(3);

            BusEvents.Count.ShouldBe(3);
        }

        [Test]
        [TestCaseSource(nameof(RetrySelectors))]
        public async Task Retry_Logic_Works_When_Exception_Source_Is_Timeout_Operator(
            Func<IObservable<Unit>, IObservable<Unit>> retrySelector) {
            var attemptCount = 0;
            var sourceWithTimeout = Observable.Defer(() => {
                attemptCount++;
                return Observable.Never<Unit>().Timeout(TimeSpan.FromMilliseconds(20));
            });

            retrySelector(sourceWithTimeout)
                .ContinueOnFault()
                .PublishFaults()
                .Subscribe();

            await Task.Delay(300);

            BusEvents.Count.ShouldBe(1);
            attemptCount.ShouldBe(3);
        }

        [Test]
        public async Task Can_Retry_inner_stream_when_outer_stream_resilient() {
            int count = 0;
            var stream = Unit.Default.Observe()
                    .SelectMany(_ => Observable.Defer(() => {
                            count++;
                            return Observable.Defer(() => Observable.Throw<Unit>(new Exception()));
                        })
                        .Retry(3)
                        .ContinueOnFault(bus => bus.Take(1).IgnoreElements()))
                ;

            var result = await stream
                .ContinueOnFault().PublishFaults()
                .Capture();
            
            await Task.Delay(1.ToSeconds());

            result.ItemCount.ShouldBe(0);
            result.Error.ShouldBeNull();
            result.IsCompleted.ShouldBeTrue();
            count.ShouldBe(3);
            BusEvents.Count.ShouldBe(1);
        }

        [Test, TestCaseSource(nameof(RetrySelectors))]
        public async Task Can_Retry_outer_when_error_in_inner(Func<IObservable<Unit>, IObservable<Unit>> retrySelector) {
            var innerItems = new List<int>();
            var innerOpSource = new Subject<int>();
            using var subscription = innerOpSource.Subscribe(innerItems.Add);

            var opBus = innerOpSource.Defer(() => Unit.Default.Observe()
                    .SelectMany(_ => {
                        innerOpSource.OnNext(1);
                        return Observable.Throw<Unit>(new Exception());
                    })
                    .ChainFaultContext())
                .ChainFaultContext(retrySelector);

            var opBusResult = await opBus.Take(3).PublishFaults().Capture();

            opBusResult.IsCompleted.ShouldBeTrue();
            innerItems.Count.ShouldBe(3);
            opBusResult.ItemCount.ShouldBe(0);
            opBusResult.Error.ShouldBeNull();
        }

        [Test]
        public void Native_RetryWhen_Is_Blind_To_Downstream_Context() {
            var exceptionTypeInRetryLogic = new List<Type>();
            var source = Observable.Throw<Unit>(new InvalidOperationException("Original Error"));

            var stream = source.RetryWhen(errors => errors
                    .Do(ex => exceptionTypeInRetryLogic.Add(ex.GetType()))
                    .Take(1)
                )
                .ChainFaultContext(handleUpstreamRetries: true);

            Should.ThrowAsync<FaultHubException>(async () => await stream.PublishFaults().LastOrDefaultAsync());

            exceptionTypeInRetryLogic.Count.ShouldBe(1);
            exceptionTypeInRetryLogic.First().ShouldBe(typeof(InvalidOperationException));
            exceptionTypeInRetryLogic.First().ShouldNotBe(typeof(FaultHubException));
        }

        [Test]
        public async Task Inner_resilient_stream_can_CompleteOnError_when_error_when_outer_stream_resilient() {
            var stream = Observable.Return(Unit.Default)
                .SelectMany(unit => unit.Defer(() => Observable.Throw<Unit>(new InvalidOperationException()))
                    .CompleteOnError(match: e => e.Has<InvalidOperationException>())
                    .ChainFaultContext().WhenCompleted())
                .ChainFaultContext();

            var result = await stream.ChainFaultContext().PublishFaults().Capture();

            result.ItemCount.ShouldBe(1);
            result.Error.ShouldBeNull();
            result.IsCompleted.ShouldBeTrue();
            BusEvents.Count.ShouldBe(0);
        }

        [Test]
        public async Task Inner_resilient_stream_can_CompleteOnFault_when_error_when_outer_stream_resilient() {
            var stream = Observable.Return(Unit.Default)
                .SelectMany(unit => unit.Defer(() => Observable.Throw<Unit>(new InvalidOperationException()))
                    .CompleteOnFault(match: e => e.Has<InvalidOperationException>())
                    .ChainFaultContext().WhenCompleted())
                .ChainFaultContext();

            var result = await stream.ChainFaultContext().PublishFaults().Capture();

            result.ItemCount.ShouldBe(1);
            result.Error.ShouldBeNull();
            result.IsCompleted.ShouldBeTrue();
            BusEvents.Count.ShouldBe(0);
        }
        
        [Test]
        public async Task RethrowOnFault_Overrides_Suppression_And_Terminates_Stream() {
            var stream = Observable.Throw<Unit>(new InvalidOperationException("Failure"))
                .ContinueOnFault(["SuppressedContext"]) 
                .RethrowOnFault();

            var result = await stream.Capture();
            
            result.Error.ShouldNotBeNull();
            result.IsCompleted.ShouldBeFalse();
            result.Error.ShouldBeOfType<FaultHubException>();
            
            BusEvents.ShouldBeEmpty();
        }
        
        [Test]
        public async Task Nested_ChainFaultContext_Correctly_Stacks_Context() {
            var innerOperation = Observable.Throw<Unit>(new InvalidOperationException("Failure"))
                .ChainFaultContext(["InnerContext"]);
            
            var fullOperation = innerOperation.ChainFaultContext(["OuterContext"]);
            
            var result = await fullOperation.PublishFaults().Capture();
            
            result.IsCompleted.ShouldBe(true);
            result.Error.ShouldBeNull();

            
            BusEvents.Count.ShouldBe(1);
            var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            
            var allContexts = fault.AllContexts.ToArray();
            var expectedContexts = new[] {
                "OuterContext",
                "InnerContext"
            };
            allContexts.ShouldBe(expectedContexts);
        }
        
        [Test]
        public async Task Inner_Operation_Failure_Triggers_Outer_Transactional_Retry() {
            var failingCounter = new SubscriptionCounter();
            var successfulCounter = new SubscriptionCounter();
            var outerCounter = new SubscriptionCounter();
            
            var result = await new object[] {Observable.Throw<string>(new InvalidOperationException("Operation Failed"))
                    .TrackSubscriptions(failingCounter), Unit.Default.Observe()
                    .TrackSubscriptions(successfulCounter) }
                .ExecuteTransaction(op => op.Retry(2), "MyTransaction")
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
            innerFault.InnerException.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Operation Failed");
        }
    
        [Test]
        public async Task When_one_stream_fails_the_other_completes() {
            var failingStream = Observable.Throw<string>(new InvalidOperationException("I have failed"));
            var succeedingStream = Observable.Timer(TimeSpan.FromMilliseconds(50)).Select(_ => "I have succeeded");
            
            var result = await failingStream.MergeResilient(succeedingStream).Capture();
            
            result.Error.ShouldBeNull();
            result.IsCompleted.ShouldBe(true);
            
            result.Items.Count.ShouldBe(1);
            result.Items.Single().ShouldBe("I have succeeded");
            
            BusEvents.Count.ShouldBe(1);
            var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            fault.InnerException.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("I have failed");
        }
        
        [Test]
        public async Task Outerstream_Operator_Takes_Over_And_Stacks_Context() {
            var opAAttemptCounter = 0;
            var opA = Observable.Defer(() => {
                    opAAttemptCounter++;
                    return Observable.Throw<int>(new InvalidOperationException("opA failed"));
                })
                .ChainFaultContext(source => source.Retry(2), ["opA"]);
            var fullChain = opA.ChainFaultContext(["opB"]);

            var result = await fullChain.PublishFaults().Capture();

            opAAttemptCounter.ShouldBe(2);
            BusEvents.Count.ShouldBe(1);
            var finalException = BusEvents.Cast<FaultHubException>().Single();
            finalException.AllContexts.Distinct()
                .ShouldBe([ "opB", "opA"]);
            result.Error.ShouldBeNull();
            result.IsCompleted.ShouldBe(true);
        }
    }
        
    }
