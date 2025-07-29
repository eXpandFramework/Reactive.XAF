using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using akarnokd.reactive_extensions;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.ExceptionExtensions;
using Xpand.Extensions.Numeric;
using Xpand.Extensions.Reactive.ErrorHandling;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Tests.FaultHubTests{
    [TestFixture]
    public class FaultHubTest:FaultHubTestBase{
        [Test]
        public void Exceptions_emitted_from_FaultHub(){
            using var observer = Observable.Throw<Unit>(new Exception()).WithFaultContext().Test();
            
            observer.ErrorCount.ShouldBe(0);
            BusObserver.ItemCount.ShouldBe(1);
            PreBusObserver.ItemCount.ShouldBe(1);
        }
        [Test]
        public async Task PreBus_Emits_Exceptions_before_Bus(){
            var error = Observable.Throw<string>(new Exception()).WithFaultContext();
            var busObserver = FaultHub.PreBus
                .SelectMany(_ => FaultHub.Bus).Test();

            await error.FirstOrDefaultAsync();
            
            busObserver.ItemCount.ShouldBe(1);
        }

        [Test]
        public void Resilient_streams_complete_and_do_not_throw(){
            var bus = Observable.Throw<Unit>(new Exception()).WithFaultContext();

            var testObserver = bus.Take(1).Test();
            
            testObserver.ErrorCount.ShouldBe(0);
            testObserver.CompletionCount.ShouldBe(1);
            BusObserver.ItemCount.ShouldBe(1);
        }
        [Test]
        public void Resilient_streams_do_not_throw_when_inner_stream_throws(){
            var bus = Observable.Defer(() => Observable.Defer(() => Observable.Throw<Unit>(new Exception()))). WithFaultContext();

            var testObserver = bus.Take(1).Test();
            
            testObserver.ErrorCount.ShouldBe(0);
            testObserver.CompletionCount.ShouldBe(1);
            BusObserver.ItemCount.ShouldBe(1);
        }
        
        [Test]
        public void Any_UpStream_emits_until_completion_when_inner_resilient_stream_throws(){
            int count = 0;
            var bus = 1.Range(3).ToObservable().Do(_ => count++)
                .SelectMany(_ => Observable.Defer(() => Observable.Throw<Unit>(new Exception())).WithFaultContext() );

            var testObserver = bus.Test();

            testObserver.ItemCount.ShouldBe(0);
            testObserver.ErrorCount.ShouldBe(0);
            testObserver.CompletionCount.ShouldBe(1);
            count.ShouldBe(3);
            
            BusObserver.ItemCount.ShouldBe(3); 
        }
        [Test]
        public void Can_Retry_inner_stream_when_outer_stream_resilient(){
            int count = 0;
            var bus = Unit.Default.Observe()
                .SelectMany(_ => Observable.Defer(() => {
                        count++;
                        return Observable.Defer(() => Observable.Throw<Unit>(new Exception()));
                        
                    })
                    )
                .WithFaultContext(s => s.Retry(3));

            var testObserver = bus.UseFaultHub().Test();

            
            testObserver.ItemCount.ShouldBe(0);
            testObserver.ErrorCount.ShouldBe(0);
            testObserver.CompletionCount.ShouldBe(1);
            count.ShouldBe(3);
            BusObserver.ItemCount.ShouldBe(1);
        }
        [Test]
        public void Inner_resilient_stream_can_CompleteOnError_when_error_when_outer_stream_resilient(){
            var bus = Unit.Default.Observe()
                .SelectManyResilient(unit => unit.Defer(() => unit.Defer(() => Observable.Throw<Unit>(new InvalidOperationException()))
                    .CompleteOnError(match:e => e.Has<InvalidOperationException>()).WhenCompleted()));

            var testObserver = bus.UseFaultHub().Test();
            
            testObserver.ItemCount.ShouldBe(1);
            testObserver.ErrorCount.ShouldBe(0);
            testObserver.CompletionCount.ShouldBe(1);
            BusObserver.ItemCount.ShouldBe(0);
        }
        [Test]
        public async Task Retry_Logic_Works_When_Exception_Source_Is_Timeout_Operator() {
            var attemptCount = 0;
            var retrySelector = (Func<IObservable<Unit>, IObservable<Unit>>)(source => source.Retry(3));
            var sourceWithTimeout = Observable.Defer(() => {
                attemptCount++;
                return Observable.Never<Unit>().Timeout(TimeSpan.FromMilliseconds(20));
            });
            
            sourceWithTimeout
                .WithFaultContext(retrySelector)
                .Subscribe();

            await Task.Delay(300);
            
            BusObserver.ItemCount.ShouldBe(1);
            attemptCount.ShouldBe(3); 
        }
        
        [Test, TestCaseSource(nameof(RetrySelectors))]
        public void Can_Retry_outer_resilient_stream_with_retry_selector_when_error_in_inner_resilient_stream(Func<IObservable<Unit>,IObservable<Unit>> retrySelector){
            var innerOpObserver = new TestObserver<int>();
            
            var opBus=innerOpObserver.Defer(() => Unit.Default.Observe()
                .SelectManyResilient(_ => {
                    innerOpObserver.OnNext(1);
                    return Observable.Throw<Unit>(new Exception());
                })).WithFaultContext(retrySelector) ;

            var opBusObserver = opBus.Take(3).Test().AwaitDone(1.ToSeconds());
            
            opBusObserver.AwaitDone(5.ToSeconds()).CompletionCount.ShouldBe(1);
            innerOpObserver.AwaitDone(3.ToSeconds()).ItemCount.ShouldBe(3);
            opBusObserver.ItemCount.ShouldBe(0);
            opBusObserver.ErrorCount.ShouldBe(0);
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
                    ).WithFaultContext(innerRetry)
            ).WithFaultContext(outerRetry);

            var opBusObserver = opBus.UseFaultHub().Test();

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
                .WithFaultContext()
                .Retry(3);

            var testObserver = bus.UseFaultHub().Test();

            
            testObserver.ItemCount.ShouldBe(0);
            testObserver.ErrorCount.ShouldBe(0);
            testObserver.CompletionCount.ShouldBe(1);
            count.ShouldBe(1);
            BusObserver.ItemCount.ShouldBe(1);
        }
        
        [Test]
        public void SelectManyResilient_op(){
            var innerOpObserver = new TestObserver<int>();
            
            var opBus = Unit.Default.Observe()
                .SelectManyResilient(_ => Observable.Defer(() => {
                    innerOpObserver.OnNext(1);
                    return Observable.Defer(() => Observable.Throw<Unit>(new Exception()));
                }))
                .WithFaultContext();

            var opBusObserver = opBus.UseFaultHub().Test();

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
                .WithFaultContext(retrySelector);

            var opBusObserver = opBus.Test();
            
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
                    return Observable.Defer(() => Observable.Throw<Unit>(new Exception())).WithFaultContext();
                })) ;

            var opBusObserver = opBus.UseFaultHub().Test();
            
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
                }).WithFaultContext(retrySelector))
                ;

            var opBusObserver = opBus.Test();

            
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
                })).WithFaultContext() ;

            var opBusObserver = opBus.Test();
            
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
                })).WithFaultContext(retrySelector) ;

            var opBusObserver = opBus.Test();
            
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
                .WithFaultContext(retrySelector)
            ) ;

            var opBusObserver = opBus.UseFaultHub().Test();
            
            opBusObserver.AwaitDone(1.ToSeconds()).CompletionCount.ShouldBe(1);
            BusObserver.ItemCount.ShouldBe(1);
            innerOpObserver.ItemCount.ShouldBe(3);
            opBusObserver.ItemCount.ShouldBe(0);
            opBusObserver.ErrorCount.ShouldBe(0);
            opBusObserver.CompletionCount.ShouldBe(1);
        }
    
    
        
    }
}