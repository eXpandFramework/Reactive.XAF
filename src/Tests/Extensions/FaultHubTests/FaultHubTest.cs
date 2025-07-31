using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using akarnokd.reactive_extensions;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.ExceptionExtensions;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Numeric;
using Xpand.Extensions.Reactive.ErrorHandling;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Tests.FaultHubTests{
    
    [TestFixture]
    public class FaultHubTest:FaultHubTestBase{
        [Test]
        public void Exceptions_emitted_from_FaultHub(){
            using var observer = Observable.Throw<Unit>(new Exception()).ChainFaultContext().PublishFaults().Test();
            
            observer.ErrorCount.ShouldBe(0);
            BusObserver.ItemCount.ShouldBe(1);
            PreBusObserver.ItemCount.ShouldBe(1);
            
            
        }
        [Test]
        public async Task PreBus_Emits_Exceptions_before_Bus(){
            var error = Observable.Throw<string>(new Exception()).ChainFaultContext();
            var busObserver = FaultHub.PreBus
                .SelectMany(_ => FaultHub.Bus).PublishFaults().Test();

            await error.PublishOnError().FirstOrDefaultAsync();
            
            busObserver.ItemCount.ShouldBe(1);
        }

        [Test]
        public void Resilient_streams_complete_and_do_not_throw(){
            var bus = Observable.Throw<Unit>(new Exception()).ChainFaultContext();

            var testObserver = bus.Take(1).PublishFaults().Test();
            
            testObserver.ErrorCount.ShouldBe(0);
            testObserver.CompletionCount.ShouldBe(1);
            BusObserver.ItemCount.ShouldBe(1);
        }
        [Test]
        public void Resilient_streams_do_not_throw_when_inner_stream_throws(){
            var bus = Observable.Defer(() => Observable.Defer(() => Observable.Throw<Unit>(new Exception()))). ChainFaultContext();

            var testObserver = bus.Take(1).PublishFaults().Test();
            
            testObserver.ErrorCount.ShouldBe(0);
            testObserver.CompletionCount.ShouldBe(1);
            BusObserver.ItemCount.ShouldBe(1);
        }
        
        [Test]
        public void Any_UpStream_emits_until_completion_when_inner_resilient_stream_throws(){
            int count = 0;
            var bus = 1.Range(3).ToObservable().Do(_ => count++)
                .SelectMany(_ => Observable.Defer(() => Observable.Throw<Unit>(new Exception())).ChainFaultContext().CompleteOnError(mute:false) );

            var testObserver = bus.PublishFaults().Test();

            testObserver.ItemCount.ShouldBe(0);
            testObserver.ErrorCount.ShouldBe(0);
            testObserver.CompletionCount.ShouldBe(1);
            count.ShouldBe(3);
            
            BusObserver.ItemCount.ShouldBe(3); 
        }

        [Test][TestCaseSource(nameof(RetrySelectors))]
        public async Task Retry_Logic_Works_When_Exception_Source_Is_Timeout_Operator(Func<IObservable<Unit>,IObservable<Unit>> retrySelector) {
            var attemptCount = 0;
            var sourceWithTimeout = Observable.Defer(() => {
                attemptCount++;
                return Observable.Never<Unit>().Timeout(TimeSpan.FromMilliseconds(20));
            });
            
            retrySelector(sourceWithTimeout)
                .ChainFaultContext()
                .PublishFaults()
                .Subscribe();

            await Task.Delay(300);
            
            BusObserver.ItemCount.ShouldBe(1);
            attemptCount.ShouldBe(3); 
        }

        [Test]
        public async Task Can_Retry_inner_stream_when_outer_stream_resilient(){
            int count = 0;
            var bus = Unit.Default.Observe()
                .SelectMany(_ => Observable.Defer(() => {
                        count++;
                        return Observable.Defer(() => Observable.Throw<Unit>(new Exception())); })
                    .Retry(3)
                    .ChainFaultContext(bus => bus.Take(1).IgnoreElements()))
                ;

            var testObserver = bus
                .ChainFaultContext().PublishFaults()
                .Test();
            await Task.Delay(1.ToSeconds());
            
            testObserver.ItemCount.ShouldBe(0);
            testObserver.ErrorCount.ShouldBe(0);
            testObserver.CompletionCount.ShouldBe(1);
            count.ShouldBe(3);
            BusObserver.ItemCount.ShouldBe(1);
        }

        [Test, TestCaseSource(nameof(RetrySelectors))]
        public void Can_Retry_outer_when_error_in_inner(Func<IObservable<Unit>,IObservable<Unit>> retrySelector){
            var innerOpObserver = new TestObserver<int>();
            
            var opBus=innerOpObserver.Defer(() => Unit.Default.Observe()
                    .SelectManyResilient(_ => {
                        innerOpObserver.OnNext(1);
                        return Observable.Throw<Unit>(new Exception());
                    }))
                .ChainFaultContext(retrySelector) ;
        
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
                .ChainFaultContext(handleUpstreamRetries:true);

            
            Should.ThrowAsync<FaultHubException>(async () => await stream.PublishFaults().LastOrDefaultAsync());

            
            exceptionTypeInRetryLogic.Count.ShouldBe(1);
            exceptionTypeInRetryLogic.First().ShouldBe(typeof(InvalidOperationException));
            exceptionTypeInRetryLogic.First().ShouldNotBe(typeof(FaultHubException));
        }
        [Test]
        public void Inner_resilient_stream_can_CompleteOnError_when_error_when_outer_stream_resilient(){
            var bus = Observable.Return(Unit.Default)
                .SelectManyResilient(unit => unit.Defer(() => Observable.Throw<Unit>(new InvalidOperationException()))
                    .CompleteOnError(match:e => e.Has<InvalidOperationException>()).WhenCompleted());

            var testObserver = bus.ChainFaultContext().PublishFaults().Test();
            
            testObserver.ItemCount.ShouldBe(1);
            testObserver.ErrorCount.ShouldBe(0);
            testObserver.CompletionCount.ShouldBe(1);
            BusObserver.ItemCount.ShouldBe(0);
        }


        [Test]
        public void Can_Compose_Nested_Retry_Strategies(){
            var innerOpObserver = new TestObserver<int>();
        
            var outerRetry = (Func<IObservable<Unit>, IObservable<Unit>>)(source => source.Retry(3));
            var innerRetry = (Func<IObservable<Unit>, IObservable<Unit>>)(source => source.Retry(2));
        
            var opBus = innerOpObserver.Defer(() => Unit.Default.Observe()
                    .SelectManyResilient(_ => {
                            innerOpObserver.OnNext(1);
                            return Observable.Throw<Unit>(new Exception());
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
        public void Cannot_Retry_resilient_stream_with_retry(){
            int count = 0;
            var bus = Unit.Default.Observe()
                .SelectManyResilient(_ => Observable.Defer(() => {
                    count++;
                    return Observable.Defer(() => Observable.Throw<Unit>(new Exception()));
                        
                }))
                
                .Retry(3);

            var testObserver = bus.PublishOnError().Test();

            
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
                .ChainFaultContext(source => source.Retry(2),["opA"] );
            
            var fullChain = opA.ChainFaultContext(["opB"]);
        
            
            var testObserver = fullChain.PublishFaults().Test();
        
            
            opAAttemptCounter.ShouldBe(2);
            BusObserver.ItemCount.ShouldBe(1);
            var finalException = BusObserver.Items.Cast<FaultHubException>().Single();
            finalException.Context.CustomContext.ShouldBe([nameof(Outerstream_Operator_Takes_Over_And_Stacks_Context), "opA", "opB"]);
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
                .ChainFaultContext(bus => bus.Retry(2),["opA"]);
            
            var opB = opA.Catch((FaultHubException ex) =>
                Observable.Throw<int>(ex).ChainFaultContext(["opB"]));
            
            var testObserver = opB.PublishFaults().Test().AwaitDone(1.ToSeconds());
            
            opAAttemptCounter.ShouldBe(2);
            BusObserver.ItemCount.ShouldBe(1);
            var finalException = BusObserver.Items.Cast<FaultHubException>().Single();
            finalException.Context.CustomContext.ShouldBe([nameof(Downstream_Operator_Takes_Over_And_Stacks_Context), "opA", "opB"]);
            testObserver.ErrorCount.ShouldBe(0);
            testObserver.CompletionCount.ShouldBe(1);
        }
        
        [Test]
        public void SelectManyResilient_op(){
            var innerOpObserver = new TestObserver<int>();
            
            var opBus = Unit.Default.Observe()
                .SelectManyResilient(_ => Observable.Defer(() => {
                    innerOpObserver.OnNext(1);
                    return Observable.Defer(() => Observable.Throw<Unit>(new Exception()));
                }))
                .ChainFaultContext();

            var opBusObserver = opBus.ChainFaultContext().PublishFaults().Test();

            opBusObserver.AwaitDone(5.ToSeconds()).CompletionCount.ShouldBe(1);
            BusObserver.ItemCount.ShouldBe(1);
            innerOpObserver.AwaitDone(1.ToSeconds()).ItemCount.ShouldBe(1);
            opBusObserver.ItemCount.ShouldBe(0);
            opBusObserver.ErrorCount.ShouldBe(0);
            opBusObserver.CompletionCount.ShouldBe(1);
            
            
        }
        
        [Test, TestCaseSource(nameof(RetrySelectors))]
        public void SelectManyResilient_can_Retry(Func<IObservable<Unit>,IObservable<Unit>> retrySelector){
            var innerOpObserver = new TestObserver<int>();
            
            var opBus = Unit.Default.Observe()
                .SelectManyResilient(_ => Observable.Defer(() => {
                    innerOpObserver.OnNext(1);
                    return Observable.Defer(() => Observable.Throw<Unit>(new Exception()));
                }))
                .ChainFaultContext(retrySelector);
        
            var opBusObserver = opBus.PublishFaults().Test();
            
            opBusObserver.AwaitDone(1.ToSeconds()).CompletionCount.ShouldBe(1);
            BusObserver.ItemCount.ShouldBe(1);
            innerOpObserver.ItemCount.ShouldBe(3);
            opBusObserver.ItemCount.ShouldBe(0);
            opBusObserver.ErrorCount.ShouldBe(0);
        }
        
        [Test]
        public void SelectManySequential_op(){
            var innerOpObserver = new TestObserver<int>();
            
            var opBus = Observable.Range(0,2)
                .SelectManySequential(_ => Observable.Defer(() => {
                    innerOpObserver.OnNext(1);
                    return Observable.Defer(() => Observable.Throw<Unit>(new Exception())).ChainFaultContext();
                }).PublishOnError()) ;

            var opBusObserver = opBus.ChainFaultContext().PublishFaults().Test();
            
            BusObserver.ItemCount.ShouldBe(2); 
            innerOpObserver.AwaitDone(1.ToSeconds()).ItemCount.ShouldBe(2);
            opBusObserver.ItemCount.ShouldBe(0);
            opBusObserver.ErrorCount.ShouldBe(0);
            opBusObserver.CompletionCount.ShouldBe(1);
        }
        
        [Test, TestCaseSource(nameof(RetrySelectors))]
        public void SelectManySequential_can_Retry(Func<IObservable<Unit>,IObservable<Unit>> retrySelector){
            var innerOpObserver = new TestObserver<int>();
            
            var opBus = Observable.Range(0,2)
                .SelectManySequential(_ => Observable.Defer(() => {
                    innerOpObserver.OnNext(1);
                    return Observable.Defer(() => Observable.Throw<Unit>(new Exception()))
                        ;
                }).ChainFaultContext(retrySelector).PublishOnError())
                ;
        
            var opBusObserver = opBus.PublishFaults().Test();
        
            
            opBusObserver.AwaitDone(1.ToSeconds()).CompletionCount.ShouldBe(1);
            BusObserver.ItemCount.ShouldBe(2);
            innerOpObserver.ItemCount.ShouldBe(6);
            opBusObserver.ItemCount.ShouldBe(0);
            opBusObserver.ErrorCount.ShouldBe(0);
            opBusObserver.CompletionCount.ShouldBe(1);
        }

        [Test]
        public void UsingResilient_op(){
            var innerOpObserver = new TestObserver<int>();
            
            var opBus = Unit.Default.Observe()
                .Using(() => new SerialDisposable(),_ => Observable.Defer(() => {
                    innerOpObserver.OnNext(1);
                    return Observable.Defer(() => Observable.Throw<Unit>(new Exception()));
                })).ChainFaultContext() ;

            var opBusObserver = opBus.PublishFaults().Test();
            
            BusObserver.ItemCount.ShouldBe(1);
            innerOpObserver.AwaitDone(1.ToSeconds()).ItemCount.ShouldBe(1);
            opBusObserver.ItemCount.ShouldBe(0);
            opBusObserver.ErrorCount.ShouldBe(0);
            opBusObserver.CompletionCount.ShouldBe(1);
        }

        [Test, TestCaseSource(nameof(RetrySelectors))]
        public void UsingResilient_op_can_retry(Func<IObservable<Unit>,IObservable<Unit>> retrySelector){
            var innerOpObserver = new TestObserver<int>();
            
            var opBus = Unit.Default.Observe()
                .Using(() => new SerialDisposable(),_ => Observable.Defer(() => {
                    innerOpObserver.OnNext(1);
                    return Observable.Defer(() => Observable.Throw<Unit>(new Exception()));
                })).ChainFaultContext(retrySelector) ;
        
            var opBusObserver = opBus.PublishFaults().Test();
            
            opBusObserver.AwaitDone(1.ToSeconds()).CompletionCount.ShouldBe(1);
            BusObserver.ItemCount.ShouldBe(1);
            innerOpObserver.ItemCount.ShouldBe(3);
            opBusObserver.ItemCount.ShouldBe(0);
            opBusObserver.ErrorCount.ShouldBe(0);
            opBusObserver.CompletionCount.ShouldBe(1);
        }

        [Test, TestCaseSource(nameof(RetrySelectors))]
        public void Can_Retry_resilient_stream_with_retry_selector_when_error_in_inner_resilient_stream(Func<IObservable<Unit>,IObservable<Unit>> retrySelector){
            var innerOpObserver = new TestObserver<int>();
            
            var opBus=innerOpObserver.Defer(() => innerOpObserver
                .Using(() => new SerialDisposable(),_ => Observable.Defer(() => {
                    innerOpObserver.OnNext(1);
                    return Observable.Defer(() => Observable.Throw<Unit>(new Exception()));
                }))
                .ChainFaultContext(retrySelector)
            ) ;
        
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
                .ChainFaultContext( retrySelector,["TOP_LEVEL_RETRY"]);
        
            resilientOperation.PublishFaults().Subscribe();
            
            attemptCount.ShouldBe(2);
            
            BusObserver.ItemCount.ShouldBe(1);
            var ex = BusObserver.Items.Single().ShouldBeOfType<FaultHubException>();
            ex.Context.CustomContext.ShouldContain("TOP_LEVEL_RETRY");
            ex.InnerException.ShouldBeOfType<InvalidOperationException>();
        }
    
        
        [Test]
        public void RethrowOnError_Causes_Resilient_Stream_To_Throw() {
            var failingSource = Observable.Throw<object>(new InvalidOperationException("Test Exception"));
            
            var testObserver = failingSource.ChainFaultContext().RethrowOnError().PublishFaults().Test();

            testObserver.ErrorCount.ShouldBe(1);
            var faultHubException = testObserver.Errors.OfType<FaultHubException>().FirstOrDefault().ShouldNotBeNull();
            var innerException = faultHubException.InnerException;
            innerException.ShouldBeOfType<InvalidOperationException>();
            innerException.Message.ShouldBe("Test Exception");
        }
        
        [Test]
        public void SelectResilient_Continues_Stream_After_Error() {
            var source = new[] { 1, 2, 3, 4 }.ToObservable();


            var testObserver = source.SelectResilient(num => {
                    if (num == 2) {
                        throw new InvalidOperationException("This is a test error.");
                    }
                    return num * 10;
                }).PublishOnError()
                .PublishFaults().Test();

            testObserver.Items.ShouldBe(new[] { 10, 30, 40 });

            
            BusObserver.ItemCount.ShouldBe(1);
            BusObserver.Items.Single().InnerException.ShouldBeOfType<InvalidOperationException>();
        }
        [Test]
        public void SelectManySequential_survives_error() {
            using var testObserver = new TestObserver<Unit>();
            using var observer = 1.Range(3).ToNowObservable()
                .Do(_ => testObserver.OnNext(Unit.Default))
                .SelectManySequential(_ => Observable.Throw<int>(new Exception()).PublishOnError())
                .PublishFaults().Test();
            
            observer.ErrorCount.ShouldBe(0);
            testObserver.ItemCount.ShouldBe(3);
            BusObserver.ItemCount.ShouldBe(3);
        }
        [Test]
        public void Defer_survives_error() {
            using var testObserver = new TestObserver<Unit>();
            using var observer = testObserver.Defer(() => {
                testObserver.OnNext(Unit.Default);
                return Observable.Throw<int>(new Exception());
            }).PublishOnError().Test();
            
            observer.ErrorCount.ShouldBe(0);
            testObserver.ItemCount.ShouldBe(1);
            BusObserver.ItemCount.ShouldBe(1);
        }

        [Test]
        public void Captures_Context_For_Simple_Resilient_Stream() {
            using var _ = this.Defer(() => Observable.Throw<Unit>(new InvalidOperationException("Error")).ChainFaultContext(["SimpleContext"]))
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
            
            var contexts = ex.Context.CustomContext.JoinCommaSpace();
            contexts.ShouldContain("Inner");
            contexts.ShouldContain("Outer");
        }
        [Test]
        public void Handles_Nested_Contexts_Correctly1() {
            this.Defer(() => Unit.Default.Observe()
                    .SelectMany(_ => Observable.Throw<Unit>(new Exception("Nested Error")).ChainFaultContext(["Outer"])))
                .PublishFaults()
                .Test();

            BusObserver.ItemCount.ShouldBe(1);
            var ex = BusObserver.Items.Single().ShouldBeOfType<FaultHubException>();
            
            ex.Context.CustomContext.ShouldContain("Outer");
        }
        [Test]
        public void Handles_Nested_Contexts_Correctly2() {
            this.Defer(() => Unit.Default.Observe()
                    .SelectManyResilient(_ => Observable.Throw<Unit>(new Exception("Nested Error"))).ChainFaultContext(["Outer"])
            ).PublishFaults().Test();

            BusObserver.ItemCount.ShouldBe(1);
            var ex = BusObserver.Items.Single().ShouldBeOfType<FaultHubException>();
            
            ex.Context.CustomContext.ShouldContain("Outer");
        }
        
        [Test]
        public async Task Works_With_Retry_Logic() {
            
            var attemptCount = 0;
            var retrySelector = (Func<IObservable<Unit>, IObservable<Unit>>)(source => source.RetryWithBackoff(3,_ => 100.ToMilliseconds()));
        
            using var _ = this.Defer(() => {
                        attemptCount++;
                        return Observable.Throw<Unit>(new InvalidOperationException("Retry Error"))
                            ;
                    }
                ).ChainFaultContext(retrySelector,["RetryContext"])
                .PublishFaults()
                .Test();
        
            await Task.Delay(1.ToSeconds());
            attemptCount.ShouldBe(3);
            BusObserver.ItemCount.ShouldBe(1);
            var ex = BusObserver.Items.Single().ShouldBeOfType<FaultHubException>();
            ex.Context.CustomContext.ShouldContain("RetryContext");
            ex.Context.CustomContext.Join().ShouldContain("Defer");
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

            
            finalException.Context.CustomContext.ShouldContain("InnerContext");
            finalException.Context.CustomContext.ShouldContain("OuterContext");
        }
        
        
    }
}