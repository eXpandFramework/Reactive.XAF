using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using akarnokd.reactive_extensions;
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
        public void Exceptions_emitted_from_FaultHub() {
            using var observer = Observable.Throw<Unit>(new Exception()).ContinueOnFault().PublishFaults().Test();

            observer.ErrorCount.ShouldBe(0);
            BusObserver.ItemCount.ShouldBe(1);
            PreBusObserver.ItemCount.ShouldBe(1);


        }

        [Test]
        public async Task PreBus_Emits_Exceptions_before_Bus() {
            var error = Observable.Throw<string>(new Exception()).ContinueOnFault();
            var busObserver = FaultHub.PreBus
                .SelectMany(_ => FaultHub.Bus).Test();

            await error.PublishFaults().FirstOrDefaultAsync();

            busObserver.ItemCount.ShouldBe(1);
        }

        [Test]
        public void Resilient_streams_complete_and_do_not_throw() {
            var bus = Observable.Throw<Unit>(new Exception()).ContinueOnFault();

            var testObserver = bus.Take(1).PublishFaults().Test();

            testObserver.ErrorCount.ShouldBe(0);
            testObserver.CompletionCount.ShouldBe(1);
            BusObserver.ItemCount.ShouldBe(1);
        }

        [Test]
        public void Resilient_streams_do_not_throw_when_inner_stream_throws() {
            var bus = Observable.Defer(() => Observable.Defer(() => Observable.Throw<Unit>(new Exception())))
                .ContinueOnFault();

            var testObserver = bus.Take(1).PublishFaults().Test();

            testObserver.ErrorCount.ShouldBe(0);
            testObserver.CompletionCount.ShouldBe(1);
            BusObserver.ItemCount.ShouldBe(1);
        }

        [Test]
        public void Any_UpStream_emits_until_completion_when_inner_resilient_stream_throws() {
            int count = 0;
            var bus = 1.Range(3).ToObservable().Do(_ => count++)
                .SelectMany(_ => Observable.Defer(() => Observable.Throw<Unit>(new Exception())).ContinueOnFault());

            var testObserver = bus.PublishFaults().Test();

            testObserver.ItemCount.ShouldBe(0);
            testObserver.ErrorCount.ShouldBe(0);
            testObserver.CompletionCount.ShouldBe(1);
            count.ShouldBe(3);

            BusObserver.ItemCount.ShouldBe(3);
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

            BusObserver.ItemCount.ShouldBe(1);
            attemptCount.ShouldBe(3);
        }

        [Test]
        public async Task Can_Retry_inner_stream_when_outer_stream_resilient() {
            int count = 0;
            var bus = Unit.Default.Observe()
                    .SelectMany(_ => Observable.Defer(() => {
                            count++;
                            return Observable.Defer(() => Observable.Throw<Unit>(new Exception()));
                        })
                        .Retry(3)
                        .ContinueOnFault(bus => bus.Take(1).IgnoreElements()))
                ;

            var testObserver = bus
                .ContinueOnFault().PublishFaults()
                .Test();
            await Task.Delay(1.ToSeconds());

            testObserver.ItemCount.ShouldBe(0);
            testObserver.ErrorCount.ShouldBe(0);
            testObserver.CompletionCount.ShouldBe(1);
            count.ShouldBe(3);
            BusObserver.ItemCount.ShouldBe(1);
        }

        [Test, TestCaseSource(nameof(RetrySelectors))]
        public void Can_Retry_outer_when_error_in_inner(Func<IObservable<Unit>, IObservable<Unit>> retrySelector) {
            var innerOpObserver = new TestObserver<int>();

            var opBus = innerOpObserver.Defer(() => Unit.Default.Observe()
                    .SelectMany(_ => {
                        innerOpObserver.OnNext(1);
                        return Observable.Throw<Unit>(new Exception());
                    })
                    .ChainFaultContext())
                .ChainFaultContext(retrySelector);

            var opBusObserver = opBus.Take(3).PublishFaults().Test().AwaitDone(4.ToSeconds());


            opBusObserver.CompletionCount.ShouldBe(1);
            innerOpObserver.ItemCount.ShouldBe(3);
            opBusObserver.ItemCount.ShouldBe(0);
            opBusObserver.ErrorCount.ShouldBe(0);
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
        public void Inner_resilient_stream_can_CompleteOnError_when_error_when_outer_stream_resilient() {
            var bus = Observable.Return(Unit.Default)
                .SelectMany(unit => unit.Defer(() => Observable.Throw<Unit>(new InvalidOperationException()))
                    .CompleteOnError(match: e => e.Has<InvalidOperationException>())
                    .ChainFaultContext().WhenCompleted())
                .ChainFaultContext();

            var testObserver = bus.ChainFaultContext().PublishFaults().Test();

            testObserver.ItemCount.ShouldBe(1);
            testObserver.ErrorCount.ShouldBe(0);
            testObserver.CompletionCount.ShouldBe(1);
            BusObserver.ItemCount.ShouldBe(0);
        }
        [Test]
        public void Inner_resilient_stream_can_CompleteOnFault_when_error_when_outer_stream_resilient() {
            var bus = Observable.Return(Unit.Default)
                .SelectMany(unit => unit.Defer(() => Observable.Throw<Unit>(new InvalidOperationException()))
                    .CompleteOnFault(match: e => e.Has<InvalidOperationException>())
                    .ChainFaultContext().WhenCompleted())
                .ChainFaultContext();

            var testObserver = bus.ChainFaultContext().PublishFaults().Test();

            testObserver.ItemCount.ShouldBe(1);
            testObserver.ErrorCount.ShouldBe(0);
            testObserver.CompletionCount.ShouldBe(1);
            BusObserver.ItemCount.ShouldBe(0);
        }

        [Test]
        public void CompleteOnError_Prevents_Composition_While_CompleteOnFault_Allows_It() {
            // ARRANGE
            var source = Observable.Throw<Unit>(new InvalidOperationException("Test Error"));

            // ACT & ASSERT for CompleteOnError (Immediate suppression prevents composition)
            // CompleteOnError immediately catches the error, so the RethrowOnFault instruction is never seen.
            var completeOnErrorStream = source
                .CompleteOnError()
                .RethrowOnFault()
                .ChainFaultContext();
            
            var testObserverOnError = completeOnErrorStream.Test();
            testObserverOnError.CompletionCount.ShouldBe(1);
            testObserverOnError.ErrorCount.ShouldBe(0);

            // ACT & ASSERT for CompleteOnFault (Declarative instructions compose)
            // Both instructions are registered. ChainFaultContext catches the error and consults the handlers.
            // The last registered handler (RethrowOnFault) wins, and the error is propagated.
            var completeOnFaultStream = source
                .CompleteOnFault()
                .RethrowOnFault()
                .ChainFaultContext();

            var testObserverOnFault = completeOnFaultStream.Test();
            testObserverOnFault.CompletionCount.ShouldBe(0);
            testObserverOnFault.ErrorCount.ShouldBe(1);
            testObserverOnFault.Errors.Single().ShouldBeOfType<FaultHubException>();
        }
        [Test]
        public void Can_Compose_Nested_Retry_Strategies() {
            var innerOpObserver = new TestObserver<int>();

            var outerRetry = (Func<IObservable<Unit>, IObservable<Unit>>)(source => source.Retry(3));
            var innerRetry = (Func<IObservable<Unit>, IObservable<Unit>>)(source => source.Retry(2));

            var opBus = innerOpObserver.Defer(() => Unit.Default.Observe()
                .SelectMany(_ => {
                        return Observable.Defer(() => {
                            innerOpObserver.OnNext(1);
                            return Observable.Throw<Unit>(new Exception());
                        }).ChainFaultContext();
                    }
                ).ChainFaultContext(innerRetry)
            ).ChainFaultContext(outerRetry);

            var opBusObserver = opBus.ChainFaultContext().PublishFaults().Test();

            opBusObserver.AwaitDone(1.ToSeconds());

            innerOpObserver.ItemCount.ShouldBe(6);
            BusObserver.ItemCount.ShouldBe(1);
            opBusObserver.CompletionCount.ShouldBe(1);
            opBusObserver.ItemCount.ShouldBe(0);
            opBusObserver.ErrorCount.ShouldBe(0);
        }

        [Test]
        public void Cannot_Retry_resilient_stream_with_retry() {
            int count = 0;
            var bus = Unit.Default.Observe()
                .SelectMany(_ => Observable.Defer(() => {
                    count++;
                    return Observable.Defer(() => Observable.Throw<Unit>(new Exception()));

                }).ContinueOnFault())

                .Retry(3);

            var testObserver = bus.PublishOnFault().Test();


            testObserver.ItemCount.ShouldBe(0);
            testObserver.ErrorCount.ShouldBe(0);
            testObserver.CompletionCount.ShouldBe(1);
            count.ShouldBe(1);
            BusObserver.ItemCount.ShouldBe(1);
        }


        [Test]
        public void Outerstream_Operator_Takes_Over_And_Stacks_Context() {
            var opAAttemptCounter = 0;
            var opA = Observable.Defer(() => {
                    opAAttemptCounter++;
                    return Observable.Throw<int>(new InvalidOperationException("opA failed"));
                })
                .ChainFaultContext(source => source.Retry(2), ["opA"]);

            var fullChain = opA.ChainFaultContext(["opB"]);


            var testObserver = fullChain.PublishFaults().Test();


            opAAttemptCounter.ShouldBe(2);
            BusObserver.ItemCount.ShouldBe(1);
            var finalException = BusObserver.Items.Cast<FaultHubException>().Single();
            finalException.AllContexts().Distinct()
                .ShouldBe([nameof(Outerstream_Operator_Takes_Over_And_Stacks_Context), "opB", "opA"]);
            testObserver.ErrorCount.ShouldBe(0);
            testObserver.CompletionCount.ShouldBe(1);
        }

        [Test]
        public void Downstream_Operator_Takes_Over_And_Stacks_Context() {
            var opAAttemptCounter = 0;
            var opA = Observable.Defer(() => {
                    opAAttemptCounter++;
                    return Observable.Throw<int>(new InvalidOperationException("opA failed"));
                })
                .ChainFaultContext(bus => bus.Retry(2), ["opA"]);

            var opB = opA.Catch((FaultHubException ex) =>
                Observable.Throw<int>(ex).ChainFaultContext(["opB"]));

            var testObserver = opB.PublishFaults().Test().AwaitDone(1.ToSeconds());

            opAAttemptCounter.ShouldBe(2);
            BusObserver.ItemCount.ShouldBe(1);
            var finalException = BusObserver.Items.Cast<FaultHubException>().Single();
            finalException.AllContexts().Distinct()
                .ShouldBe([nameof(Downstream_Operator_Takes_Over_And_Stacks_Context), "opB", "opA"]);
            testObserver.ErrorCount.ShouldBe(0);
            testObserver.CompletionCount.ShouldBe(1);
        }


        [Test, TestCaseSource(nameof(RetrySelectors))]
        public void Can_Retry_resilient_stream_with_retry_selector_when_error_in_inner_resilient_stream(
            Func<IObservable<Unit>, IObservable<Unit>> retrySelector) {
            var innerOpObserver = new TestObserver<int>();

            var opBus = innerOpObserver.Defer(() => innerOpObserver
                .Using(() => new SerialDisposable(), _ => Observable.Defer(() => {
                    innerOpObserver.OnNext(1);
                    return Observable.Defer(() => Observable.Throw<Unit>(new Exception()));
                }))
                .ChainFaultContext(retrySelector)
            );

            var opBusObserver = opBus.ChainFaultContext().PublishFaults().Test();

            opBusObserver.AwaitDone(1.ToSeconds()).CompletionCount.ShouldBe(1);
            BusObserver.ItemCount.ShouldBe(1);
            innerOpObserver.ItemCount.ShouldBe(3);
            opBusObserver.ItemCount.ShouldBe(0);
            opBusObserver.ErrorCount.ShouldBe(0);
            opBusObserver.CompletionCount.ShouldBe(1);
        }

        [Test]
        public void MakeResilient_WithTopLevelRetry_CorrectlyRetriesAndPublishes() {
            var attemptCount = 0;
            var failingOperation = Observable.Defer(() => {
                attemptCount++;
                return Observable.Throw<int>(new InvalidOperationException("Operation Failed"));
            });

            var retrySelector = (Func<IObservable<int>, IObservable<int>>)(source => source.Retry(2));

            var resilientOperation = failingOperation
                .ChainFaultContext(retrySelector, ["TOP_LEVEL_RETRY"]);

            resilientOperation.PublishFaults().Subscribe();

            attemptCount.ShouldBe(2);

            BusObserver.ItemCount.ShouldBe(1);
            var ex = BusObserver.Items.Single().ShouldBeOfType<FaultHubException>();
            ex.Context.CustomContext.ShouldContain("TOP_LEVEL_RETRY");
            ex.InnerException.ShouldBeOfType<InvalidOperationException>();
        }


        [Test]
        public void RethrowOnFault_Causes_Resilient_Stream_To_Throw() {
            var failingSource = Observable.Throw<object>(new InvalidOperationException("Test Exception"));

            var testObserver = failingSource.ContinueOnFault().RethrowOnFault().Test();

            testObserver.ErrorCount.ShouldBe(1);
            var faultHubException = testObserver.Errors.OfType<FaultHubException>().FirstOrDefault().ShouldNotBeNull();
            var innerException = faultHubException.InnerException;
            innerException.ShouldBeOfType<InvalidOperationException>();
            innerException.Message.ShouldBe("Test Exception");
        }

        [Test]
        public void SelectResilient_Continues_Stream_After_Error() {
            var source = new[] { 1, 2, 3, 4 }.ToObservable();


            var testObserver = source.SelectItemResilient(num => {
                    if (num == 2) {
                        throw new InvalidOperationException("This is a test error.");
                    }

                    return num * 10;
                }).PublishOnFault()
                .PublishFaults().Test();

            testObserver.Items.ShouldBe(new[] { 10, 30, 40 });


            BusObserver.ItemCount.ShouldBe(1);
            BusObserver.Items.Single().InnerException.ShouldBeOfType<InvalidOperationException>();
        }
        
        [Test]
        public void Defer_survives_error() {
            using var testObserver = new TestObserver<Unit>();
            using var observer = testObserver.Defer(() => {
                testObserver.OnNext(Unit.Default);
                return Observable.Throw<int>(new Exception());
            }).ChainFaultContext().PublishOnFault().Test();

            observer.ErrorCount.ShouldBe(0);
            testObserver.ItemCount.ShouldBe(1);
            BusObserver.ItemCount.ShouldBe(1);
        }

        [Test]
        public void Captures_Context_For_Simple_Resilient_Stream() {
            using var _ = this.Defer(()
                    => Observable.Throw<Unit>(new InvalidOperationException("Error"))
                        .ChainFaultContext(["SimpleContext"]))
                .PublishFaults()
                .Test();

            BusObserver.ItemCount.ShouldBe(1);
            var ex = BusObserver.Items.Single().ShouldBeOfType<FaultHubException>();
            ex.Context.CustomContext.ShouldContain("SimpleContext");
            ex.InnerException.ShouldBeOfType<InvalidOperationException>();
        }

        [Test]
        public void Handles_Nested_Contexts_Correctly() {
            this.Defer(() =>
                    Unit.Default.Observe()
                        .SelectMany(_ =>
                            Observable.Throw<Unit>(new Exception("Nested Error"))
                                .ChainFaultContext(["Inner"])
                        )
                        .ChainFaultContext(["Outer"])
                )
                .PublishFaults().Test();

            BusObserver.ItemCount.ShouldBe(1);
            var ex = BusObserver.Items.Single().ShouldBeOfType<FaultHubException>();

            ex.AllContexts().Distinct().ShouldBe([nameof(Handles_Nested_Contexts_Correctly), "Outer", "Inner"]);
        }

        [Test]
        public void Handles_Nested_Contexts_Correctly1() {
            this.Defer(() => Unit.Default.Observe()
                    .SelectMany(_
                        => Observable.Throw<Unit>(new Exception("Nested Error")).ChainFaultContext(["Outer"])))
                .PublishFaults()
                .Test();

            BusObserver.ItemCount.ShouldBe(1);
            var ex = BusObserver.Items.Single().ShouldBeOfType<FaultHubException>();

            ex.Context.CustomContext.ShouldContain("Outer");
        }

        [Test]
        public void Handles_Nested_Contexts_Correctly2() {
            this.Defer(() => Unit.Default.Observe()
                .SelectMany(_
                    => this.Defer(() => Observable.Throw<Unit>(new Exception("Nested Error"))).ChainFaultContext())
                .ChainFaultContext(["Outer"])
            ).PublishFaults().Test();

            BusObserver.ItemCount.ShouldBe(1);
            var ex = BusObserver.Items.Single().ShouldBeOfType<FaultHubException>();

            ex.Context.CustomContext.ShouldContain("Outer");
        }

        [Test]
        public void Works_With_Retry_Logic() {
            var attemptCount = 0;
            var retrySelector =
                (Func<IObservable<Unit>, IObservable<Unit>>)(source
                    => source.RetryWithBackoff(3, _ => 100.ToMilliseconds()));

            using var _ = this.Defer(() => {
                        attemptCount++;
                        return Observable.Throw<Unit>(new InvalidOperationException("Retry Error"))
                            ;
                    }
                ).ChainFaultContext(retrySelector, ["RetryContext"])
                .PublishFaults()
                .Test().AwaitDone(1.ToSeconds());

            attemptCount.ShouldBe(3);
            BusObserver.ItemCount.ShouldBe(1);
            var ex = BusObserver.Items.Single().ShouldBeOfType<FaultHubException>();
            ex.Context.CustomContext.ShouldContain("RetryContext");
            ex.Context.CustomContext.ShouldContain(nameof(Works_With_Retry_Logic));
        }

        [Test]
        public void Nested_WithFaultContext_Without_Retry_Should_Stack_Contexts() {
            var nestedResilientStream =
                Observable.Defer(() =>
                        Observable.Throw<Unit>(new InvalidOperationException("Inner Error"))
                            .ChainFaultContext(["InnerContext"])
                    )
                    .ChainFaultContext(["OuterContext"]);

            using var _ = nestedResilientStream.PublishFaults().Test();

            BusObserver.ItemCount.ShouldBe(1);
            var finalException = BusObserver.Items.Single().ShouldBeOfType<FaultHubException>();

            finalException.AllContexts().Distinct().ShouldBe([
                nameof(Nested_WithFaultContext_Without_Retry_Should_Stack_Contexts), "OuterContext", "InnerContext"
            ]);
        }

        [Test]
        public void ChainFaultContext_Handles_Disposal_Exception_From_Using() {
            var resource = new TestResource
                { OnDispose = () => throw new InvalidOperationException("Dispose failed.") };

            var testObserver = Observable.Using(() => resource, _ => Observable.Return(42))
                .ChainFaultContext(["UsingTest"])
                .Test();

            testObserver.Items.ShouldBe(new[] { 42 });
            testObserver.CompletionCount.ShouldBe(1);
            resource.IsDisposed.ShouldBeTrue();
            BusObserver.ItemCount.ShouldBe(1);
            var fault = BusObserver.Items.Single().ShouldBeOfType<FaultHubException>();
            fault.InnerException.ShouldBeOfType<InvalidOperationException>();
            fault.Context.CustomContext.ShouldContain("UsingTest");
        }

        [Test]
        public void ChainFaultContext_Handles_Disposal_Exception_From_Deferred_Stream() {
            var resource = new TestResource
                { OnDispose = () => throw new InvalidOperationException("Dispose failed.") };
            var factory =
                new Func<IObservable<int>>(() => Observable.Using(() => resource, _ => Observable.Return(42)));

            var testObserver = Observable.Defer(factory)
                .ChainFaultContext(["DeferTest"])
                .Test();

            testObserver.Items.ShouldBe(new[] { 42 });
            testObserver.CompletionCount.ShouldBe(1);
            resource.IsDisposed.ShouldBeTrue();
            BusObserver.ItemCount.ShouldBe(1);
            var fault = BusObserver.Items.Single().ShouldBeOfType<FaultHubException>();
            fault.InnerException.ShouldBeOfType<InvalidOperationException>();
            fault.Context.CustomContext.ShouldContain("DeferTest");
        }

        [Test]
        public void ChainFaultContext_Correctly_Enriches_Disposal_Exception_With_Context() {
            var resource = new TestResource
                { OnDispose = () => throw new InvalidOperationException("Dispose failed.") };
            var sourceWithFailingDispose = Observable.Using(() => resource, _ => Observable.Return(42));

            var testObserver = sourceWithFailingDispose
                .ChainFaultContext(["MyContext"])
                .Test();

            testObserver.Items.ShouldBe(new[] { 42 });
            testObserver.CompletionCount.ShouldBe(1);
            testObserver.ErrorCount.ShouldBe(0);

            BusObserver.ItemCount.ShouldBe(1);
            var fault = BusObserver.Items.Single().ShouldBeOfType<FaultHubException>();
            fault.InnerException.ShouldBeOfType<InvalidOperationException>();

            fault.Context.CustomContext.ShouldContain("MyContext");
        }

        [Test]
        public void LowLevel_ItemResilience_Does_Not_Lose_Outer_Context_When_Bookmark_Is_Used() {
            var source = Observable.Return(Unit.Default);
            var innerFailingStreamWithBookmark = Observable
                .Throw<Unit>(new InvalidOperationException("Inner failure"))
                .ChainFaultContext(["InnerContext"]);

            using var testObserver = source
                .SelectMany(_ => innerFailingStreamWithBookmark.ContinueOnFault(["OuterContext"]))
                .PublishFaults()
                .Test();

            BusObserver.ItemCount.ShouldBe(1);
            var fault = BusObserver.Items.Single().ShouldBeOfType<FaultHubException>();
            fault.AllContexts().Distinct().ShouldBe([
                nameof(LowLevel_ItemResilience_Does_Not_Lose_Outer_Context_When_Bookmark_Is_Used), "OuterContext",
                "InnerContext"
            ]);
        }


        [Test]
        public void Preserves_Origin_StackTrace_For_Synchronous_Exception_Without_StackTrace() {
            var source = Observable.Throw<Unit>(new InvalidOperationException("Stackless fail"));

            source.ContinueOnFault().Subscribe();

            BusObserver.ItemCount.ShouldBe(1);
            var fault = BusObserver.Items.Single().ShouldBeOfType<FaultHubException>();

            var output = fault.ToString();

            output.ShouldContain("--- Stack Trace (from innermost fault context) ---");

            output.ShouldContain(nameof(Preserves_Origin_StackTrace_For_Synchronous_Exception_Without_StackTrace));
        }


        [Test]
        public void Sequential_Operations_With_Failures_Trigger_Outer_Retry() {
            var op1Attempts = 0;
            var op2Attempts = 0;
            var op3Attempts = 0;
            var outerRetries = 0;

            var source1 = Observable.Defer(() => {
                op1Attempts++;
                return Observable.Throw<int>(new InvalidOperationException("Op1 Failed"));
            }).ChainFaultContext(s => s.Retry(2), ["Op1"]);

            var source2 = Observable.Defer(() => {
                op2Attempts++;
                return Observable.Throw<decimal>(new InvalidOperationException("Op2 Failed"));
            }).ChainFaultContext(s => s.Retry(2), ["Op2"]);

            var source3 = Observable.Defer(() => {
                op3Attempts++;
                return this.Observe();
            }).ChainFaultContext(s => s.Retry(2), ["Op3"]);

            var operations = new [] { source1.ToTransactional(), source2.ToTransactional(), source3.ToTransactional() };

            var fullSequence = Observable.Defer(() => {
                    outerRetries++;
                    return operations.ExecuteTransaction("Sequence");
                })
                .ChainFaultContext(s => s.Retry(3), ["Outer"]);

            using var testObserver = fullSequence.PublishFaults().Test();
            testObserver.AwaitDone(TimeSpan.FromSeconds(5));

            outerRetries.ShouldBe(3);

            op1Attempts.ShouldBe(6); 
            op2Attempts.ShouldBe(6); 
            op3Attempts.ShouldBe(3); 

            BusObserver.ItemCount.ShouldBe(1);
            var fault = BusObserver.Items.Single().ShouldBeOfType<FaultHubException>();
            fault.AllContexts().ShouldContain("Outer");
            fault.InnerException.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Sequence failed");

            testObserver.CompletionCount.ShouldBe(1);
            testObserver.ErrorCount.ShouldBe(0);
        }
        
        [Test]
        public void ChainFaultContext_With_Retry_Propagates_Error_To_Subsequent_Catch() {
            var subscriptionAttempts = 0;
            var catchBlockReached = false;

            var failingSource = Observable.Defer(() => {
                subscriptionAttempts++;
                return Observable.Throw<Unit>(new InvalidOperationException("Persistent Failure"));
            });

            var resilientStream = failingSource
                .ChainFaultContext(s => s.Retry(3), ["RetryContext"])
                .ToUnit()
                .Catch<Unit, Exception>(e => {
                    catchBlockReached = true;
                    var fault = e.ShouldBeOfType<FaultHubException>();
                    fault.AllContexts().ShouldContain("RetryContext");
                    return Observable.Empty<Unit>();
                });

            using var testObserver = resilientStream.SubscribeOn(NewThreadScheduler.Default).Test();
            testObserver.AwaitDone(TimeSpan.FromSeconds(5));

            subscriptionAttempts.ShouldBe(3);
            catchBlockReached.ShouldBe(true);
            testObserver.CompletionCount.ShouldBe(1);
            testObserver.ErrorCount.ShouldBe(0);
        }
    }
}