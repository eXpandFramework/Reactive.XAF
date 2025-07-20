using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using akarnokd.reactive_extensions;
using DevExpress.ExpressApp.Win;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Numeric;
using Xpand.Extensions.Reactive.ErrorHandling;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.XAF.Modules.Reactive.Tests{
    [TestFixture]
    public class FaultHubTests{
        private TestObserver<Exception> _busObserver;
        private TestObserver<Exception> _preBusObserver;
        private static IEnumerable<TestCaseData> RetrySelectors() {
            yield return new TestCaseData(RetrySelector).SetName(nameof(RetrySelector));
            yield return new TestCaseData(RetrySelectorWithBackoff).SetName(nameof(RetrySelectorWithBackoff));
        }

        private static Func<IObservable<Unit>,IObservable<Unit>> RetrySelector=>source => source.Retry(3);
        private static Func<IObservable<Unit>,IObservable<Unit>> RetrySelectorWithBackoff=>source => source.RetryWithBackoff(3, strategy:_ => 100.Milliseconds());
        [SetUp]
        public void Setup(){
         _busObserver = FaultHub.Bus.Test();
         _preBusObserver = FaultHub.PreBus.Test();
        }


        [Test]
        public void Exceptions_emitted_from_FaultHub(){
            using var observer = Observable.Throw<Unit>(new Exception()).ToResilient() .UseFaultHub().Test();
            
            observer.ErrorCount.ShouldBe(0);
            _busObserver.ItemCount.ShouldBe(1);
            _preBusObserver.ItemCount.ShouldBe(1);
        }
        [Test]
        public async Task PreBus_Emits_Exceptions_before_Bus(){
            var error = Observable.Throw<string>(new Exception()).UseFaultHub();
            var busObserver = FaultHub.PreBus
                .SelectMany(_ => FaultHub.Bus).UseFaultHub().Test();

            await error.FirstOrDefaultAsync();
            
            busObserver.ItemCount.ShouldBe(1);
        }

        [Test]
        public void Resilient_streams_complete_and_do_not_throw(){
            var bus = Observable.Throw<Unit>(new Exception()).ToResilient();

            var testObserver = bus.Take(1).UseFaultHub().Test();
            
            testObserver.ErrorCount.ShouldBe(0);
            testObserver.CompletionCount.ShouldBe(1);
            _busObserver.ItemCount.ShouldBe(1);
        }
        [Test]
        public void Resilient_streams_do_not_throw_when_inner_stream_throws(){
            var bus = Observable.Defer(() => Observable.Defer(() => Observable.Throw<Unit>(new Exception()))). ToResilient();

            var testObserver = bus.Take(1).UseFaultHub().Test();
            
            testObserver.ErrorCount.ShouldBe(0);
            testObserver.CompletionCount.ShouldBe(1);
            _busObserver.ItemCount.ShouldBe(1);
        }
        
        [Test]
        public void Any_UpStream_emits_until_completion_when_inner_resilient_stream_throws(){
            int count = 0;
            var bus = 1.Range(3).ToObservable().Do(_ => count++)
                .SelectMany(_ => Observable.Defer(() => Observable.Throw<Unit>(new Exception())).ToResilient() );

            var testObserver = bus.UseFaultHub().Test();

            testObserver.ItemCount.ShouldBe(0);
            testObserver.ErrorCount.ShouldBe(0);
            testObserver.CompletionCount.ShouldBe(1);
            count.ShouldBe(3);
            
            _busObserver.ItemCount.ShouldBe(3); //fails here 3!=1
        }
        [Test]
        public void Can_Retry_inner_stream_when_outer_stream_resilient(){
            int count = 0;
            var bus = Unit.Default.Observe()
                .SelectManyResilient(_ => Observable.Defer(() => {
                        count++;
                        return Observable.Defer(() => Observable.Throw<Unit>(new Exception()));
                        
                    })
                    .Retry(3) );

            var testObserver = bus.UseFaultHub().Test();

            
            testObserver.ItemCount.ShouldBe(0);
            testObserver.ErrorCount.ShouldBe(0);
            testObserver.CompletionCount.ShouldBe(1);
            count.ShouldBe(3);
            _busObserver.ItemCount.ShouldBe(1);
        }
        [Test]
        public void Inner_resilient_stream_can_CompleteOnError_when_error_when_outer_stream_resilient(){
            var bus = Unit.Default.Observe()
                .SelectManyResilient(unit => unit.Defer(() => unit.Defer(() => Observable.Throw<Unit>(new InvalidOperationException())
                    ,caller:"caller0").CompleteOnError(match:e => e is InvalidOperationException).WhenCompleted(),caller:"caller1"));

            var testObserver = bus.UseFaultHub().Test();

            
            testObserver.ItemCount.ShouldBe(1);
            testObserver.ErrorCount.ShouldBe(0);
            testObserver.CompletionCount.ShouldBe(1);
            _busObserver.ItemCount.ShouldBe(0);
        }
        
        [Test, TestCaseSource(nameof(RetrySelectors))]
        public void Can_Retry_outer_resilient_stream_with_retry_selector_when_error_in_inner_resilient_stream(Func<IObservable<Unit>,IObservable<Unit>> retrySelector){
            var innerOpObserver = new TestObserver<int>();
            
            var opBus=innerOpObserver.Defer(() => Unit.Default.Observe()
                .SelectManyResilient(_ => {
                    innerOpObserver.OnNext(1);
                    return Observable.Throw<Unit>(new Exception());
                }),retrySelector) ;

            var opBusObserver = opBus.UseFaultHub().Test();
            
            opBusObserver.AwaitDone(1.ToSeconds()).CompletionCount.ShouldBe(1);
            _busObserver.ItemCount.ShouldBe(1);
            innerOpObserver.ItemCount.ShouldBe(3);
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
                        },
                        retrySelector: innerRetry
                    ),
                retrySelector: outerRetry
            );

            var opBusObserver = opBus.UseFaultHub().Test();

            opBusObserver.AwaitDone(1.ToSeconds());

            innerOpObserver.ItemCount.ShouldBe(6);
            _busObserver.ItemCount.ShouldBe(1);
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

            var testObserver = bus.UseFaultHub().Test();

            
            testObserver.ItemCount.ShouldBe(0);
            testObserver.ErrorCount.ShouldBe(0);
            testObserver.CompletionCount.ShouldBe(1);
            count.ShouldBe(1);
            _busObserver.ItemCount.ShouldBe(1);
        }
        
        [Test]
        public void SelectManyResilient_op(){
            var innerOpObserver = new TestObserver<int>();
            
            var opBus = Unit.Default.Observe()
                .SelectManyResilient(_ => Observable.Defer(() => {
                        innerOpObserver.OnNext(1);
                        return Observable.Defer(() => Observable.Throw<Unit>(new Exception()));
                    })) ;

            var opBusObserver = opBus.UseFaultHub().Test();

            opBusObserver.AwaitDone(5.ToSeconds()).CompletionCount.ShouldBe(1);
            _busObserver.ItemCount.ShouldBe(1);
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
                    }),retrySelector) ;

            var opBusObserver = opBus.UseFaultHub().Test();
            
            opBusObserver.AwaitDone(1.ToSeconds()).CompletionCount.ShouldBe(1);
            _busObserver.ItemCount.ShouldBe(1);
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
                        return Observable.Defer(() => Observable.Throw<Unit>(new Exception()));
                    })) ;

            var opBusObserver = opBus.UseFaultHub().Test();
            
            _busObserver.ItemCount.ShouldBe(2); //return 1 and fails
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
                        return Observable.Defer(() => Observable.Throw<Unit>(new Exception()));
                    }),retrySelector) ;

            var opBusObserver = opBus.UseFaultHub().Test();

            
            opBusObserver.AwaitDone(1.ToSeconds()).CompletionCount.ShouldBe(1);
            _busObserver.ItemCount.ShouldBe(2);
            innerOpObserver.ItemCount.ShouldBe(6);
            opBusObserver.ItemCount.ShouldBe(0);
            opBusObserver.ErrorCount.ShouldBe(0);
            opBusObserver.CompletionCount.ShouldBe(1);
        }

        [Test]
        public void UsingResilient_op(){
            var innerOpObserver = new TestObserver<int>();
            
            var opBus = Unit.Default.Observe()
                .UsingResilient(() => new WinApplication(),_ => Observable.Defer(() => {
                    innerOpObserver.OnNext(1);
                    return Observable.Defer(() => Observable.Throw<Unit>(new Exception()));
                })) ;

            var opBusObserver = opBus.UseFaultHub().Test();
            
            _busObserver.ItemCount.ShouldBe(1);
            innerOpObserver.AwaitDone(1.ToSeconds()).ItemCount.ShouldBe(1);
            opBusObserver.ItemCount.ShouldBe(0);
            opBusObserver.ErrorCount.ShouldBe(0);
            opBusObserver.CompletionCount.ShouldBe(1);
        }

        [Test, TestCaseSource(nameof(RetrySelectors))]
        public void UsingResilient_op_can_retry(Func<IObservable<Unit>,IObservable<Unit>> retrySelector){
            var innerOpObserver = new TestObserver<int>();
            
            var opBus = Unit.Default.Observe()
                .UsingResilient(() => new WinApplication(),_ => Observable.Defer(() => {
                    innerOpObserver.OnNext(1);
                    return Observable.Defer(() => Observable.Throw<Unit>(new Exception()));
                }),retrySelector) ;

            var opBusObserver = opBus.UseFaultHub().Test();
            
            opBusObserver.AwaitDone(1.ToSeconds()).CompletionCount.ShouldBe(1);
            _busObserver.ItemCount.ShouldBe(1);
            innerOpObserver.ItemCount.ShouldBe(3);
            opBusObserver.ItemCount.ShouldBe(0);
            opBusObserver.ErrorCount.ShouldBe(0);
            opBusObserver.CompletionCount.ShouldBe(1);
        }

        [Test, TestCaseSource(nameof(RetrySelectors))]
        public void Can_Retry_resilient_stream_with_retry_selector_when_error_in_inner_resilient_stream(Func<IObservable<Unit>,IObservable<Unit>> retrySelector){
            var innerOpObserver = new TestObserver<int>();
            
            var opBus=innerOpObserver.Defer(() => innerOpObserver
                .UsingResilient(() => new WinApplication(),_ => Observable.Defer(() => {
                    innerOpObserver.OnNext(1);
                    return Observable.Defer(() => Observable.Throw<Unit>(new Exception()));
                })),retrySelector) ;

            var opBusObserver = opBus.UseFaultHub().Test();
            
            opBusObserver.AwaitDone(1.ToSeconds()).CompletionCount.ShouldBe(1);
            _busObserver.ItemCount.ShouldBe(1);
            innerOpObserver.ItemCount.ShouldBe(3);
            opBusObserver.ItemCount.ShouldBe(0);
            opBusObserver.ErrorCount.ShouldBe(0);
            opBusObserver.CompletionCount.ShouldBe(1);        }
    }
    
}

