using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using akarnokd.reactive_extensions;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Numeric;
using Xpand.Extensions.Reactive.ErrorHandling;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.TestsLib.Common;

namespace Xpand.Extensions.Tests.FaultHubTests{
    public class OperatorFaultTests:FaultHubTestBase {
        [Test]
        public void SafeguardDisposal_Handles_Late_Disposal_Exception_By_Publishing_To_FaultHub() {
            var resource = new TestResource { OnDispose = () => throw new InvalidOperationException("Late dispose failed.") };
            var sourceWithFailingDispose = Observable.Using(() => resource, _ => Observable.Never<int>());
            
            var subscription = sourceWithFailingDispose.SafeguardSubscription((e, s) => e.ExceptionToPublish(new object[]{"LateDisposalTest"}.NewFaultContext(s)).Publish()).Test();
            
            subscription.Dispose();
            
            
            subscription.ItemCount.ShouldBe(0);
            subscription.CompletionCount.ShouldBe(0);
            subscription.ErrorCount.ShouldBe(0);
            
            resource.IsDisposed.ShouldBeTrue();
            
            BusObserver.ItemCount.ShouldBe(1);
            var fault = BusObserver.Items.Single().ShouldBeOfType<FaultHubException>();
            fault.InnerException.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Late dispose failed.");
            fault.Context.CustomContext.ShouldContain("LateDisposalTest");
        }
        [Test]
        public void SafeguardDisposal_Handles_Exception_From_Dispose_By_Publishing_To_FaultHub() {
            var resource = new TestResource { OnDispose = () => throw new InvalidOperationException("Dispose failed.") };
            var sourceWithFailingDispose = Observable.Using(() => resource, _ => Observable.Return(42));
            
            var testObserver = sourceWithFailingDispose.SafeguardSubscription((e, s) => e.ExceptionToPublish(new object[]{"SafeguardTest"}.NewFaultContext(s)).Publish()).Test();
            
            testObserver.Items.ShouldBe(new[] { 42 });
            testObserver.CompletionCount.ShouldBe(1);
            testObserver.ErrorCount.ShouldBe(0);
            
            resource.IsDisposed.ShouldBeTrue();
            
            BusObserver.ItemCount.ShouldBe(1);
            var fault = BusObserver.Items.Single().ShouldBeOfType<FaultHubException>();
            fault.InnerException.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Dispose failed.");
            fault.Context.CustomContext.ShouldContain("SafeguardTest");
        }
            
        [Test]
        public void DeferItemResilient_Executes_Successfully_When_Factory_Does_Not_Throw() {
            var factory = new Func<IObservable<int>>(() => Observable.Return(42));
            
            var testObserver = factory.DeferItemResilient(factory, ["HappyPath"])
                .PublishFaults()
                .Test();
            
            testObserver.Items.ShouldBe(new[] { 42 });
            testObserver.ErrorCount.ShouldBe(0);
            testObserver.CompletionCount.ShouldBe(1);
            BusObserver.ItemCount.ShouldBe(0);
        }

        [Test]
        public void DeferItemResilient_Handles_Synchronous_Exception_From_Factory() {
            
            var factory = new Func<IObservable<int>>(() => throw new InvalidOperationException("Factory failed synchronously."));
            var context = new object[] { "SyncError" };

            
            var testObserver = factory.DeferItemResilient(factory, context)
                .PublishFaults()
                .Test();

            
            testObserver.ItemCount.ShouldBe(0);
            testObserver.ErrorCount.ShouldBe(0);
            testObserver.CompletionCount.ShouldBe(1);

            BusObserver.ItemCount.ShouldBe(1);
            var fault = BusObserver.Items.Single().ShouldBeOfType<FaultHubException>();
            fault.InnerException.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Factory failed synchronously.");
            fault.Context.CustomContext.ShouldContain("SyncError");
        }

        [Test]
        public async Task DeferItemResilient_Handles_Asynchronous_Exception_From_Produced_Stream() {
            
            var factory = new Func<IObservable<int>>(() =>
                Observable.Timer(50.Milliseconds())
                          .SelectMany(_ => Observable.Throw<int>(new InvalidOperationException("Stream failed asynchronously.")))
            );
            var context = new object[] { "AsyncError" };

            
            var testObserver = factory.DeferItemResilient(factory, context)
                .PublishFaults()
                .Test();

            await testObserver.AwaitDoneAsync(1.Seconds());
            
            
            testObserver.ItemCount.ShouldBe(0);
            testObserver.ErrorCount.ShouldBe(0);
            testObserver.CompletionCount.ShouldBe(1);

            BusObserver.ItemCount.ShouldBe(1);
            var fault = BusObserver.Items.Single().ShouldBeOfType<FaultHubException>();
            fault.InnerException.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Stream failed asynchronously.");
            fault.Context.CustomContext.ShouldContain("AsyncError");
        }
        
        [Test]
        public void DeferItemResilient_Handles_Disposal_Exception_From_Hosted_Stream() {
            // ARRANGE
            var resource = new TestResource { OnDispose = () => throw new InvalidOperationException("Dispose failed.") };
            // Create a factory that produces an observable with a faulting disposal phase.
            var factory = new Func<IObservable<int>>(() => Observable.Using(() => resource, _ => Observable.Return(42)));

            // ACT
            // DeferItemResilient should host this observable and handle its disposal exception.
            var testObserver = this.DeferItemResilient(factory, ["DisposeFail"]).Test();

            // ASSERT
            // The subscriber should receive the value and a completion signal.
            testObserver.Items.ShouldBe(new[] { 42 });
            testObserver.CompletionCount.ShouldBe(1);
            testObserver.ErrorCount.ShouldBe(0); // No unhandled error.

            // The resource should have been disposed.
            resource.IsDisposed.ShouldBeTrue();

            // The exception from the Dispose method should have been published to the FaultHub.
            BusObserver.ItemCount.ShouldBe(1);
            var fault = BusObserver.Items.Single().ShouldBeOfType<FaultHubException>();
            fault.InnerException.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Dispose failed.");
            fault.Context.CustomContext.ShouldContain("DisposeFail");
        }
        
        [Test][Obsolete]
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
        
        [Test, TestCaseSource(nameof(RetrySelectors))][Obsolete]
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

        [Test]
        public void DoItemResilient_Continues_Stream_After_Error_And_Does_Not_Filter_Items() {
            var source = Enumerable.Range(1, 4).ToObservable();
            var successfullyProcessed = new List<int>();
            
            var testObserver = source.DoItemResilient(num => {
                    if (num == 2) {
                        throw new InvalidOperationException("This is a test error.");
                    }
                    successfullyProcessed.Add(num);
                })
                .PublishFaults() 
                .Test();

            
            testObserver.Items.ShouldBe(new[] { 1, 2, 3, 4 });
            testObserver.ErrorCount.ShouldBe(0);
            testObserver.CompletionCount.ShouldBe(1);

            
            successfullyProcessed.ShouldBe(new[] { 1, 3, 4 });
            
            BusObserver.ItemCount.ShouldBe(1);
            BusObserver.Items.Single().InnerException.ShouldBeOfType<InvalidOperationException>();
        }


        [Test]
        public void UsingItemResilient_Handles_ResourceFactory_Exception() {
            // ARRANGE
            var resourceFactory = new Func<TestResource>(() => throw new InvalidOperationException("Resource factory failed."));
            var observableFactory = new Func<TestResource, IObservable<int>>(_ => Observable.Return(42));

            // ACT
            var testObserver = this.UsingItemResilient(resourceFactory, observableFactory, ["ResourceFail"])
                .PublishFaults()
                .Test();

            // ASSERT
            testObserver.CompletionCount.ShouldBe(1);
            testObserver.ErrorCount.ShouldBe(0);
            BusObserver.ItemCount.ShouldBe(1);
            BusObserver.Items.Single().InnerException.ShouldBeOfType<InvalidOperationException>();
        }

        [Test]
        public void UsingItemResilient_Handles_ObservableFactory_Exception() {
            // ARRANGE
            var resource = new TestResource();
            var resourceFactory = new Func<TestResource>(() => resource);
            var observableFactory = new Func<TestResource, IObservable<int>>(_ => throw new InvalidOperationException("Observable factory failed."));

            // ACT
            var testObserver = this.UsingItemResilient(resourceFactory, observableFactory, ["ObservableFail"])
                .PublishFaults()
                .Test();

            // ASSERT
            testObserver.CompletionCount.ShouldBe(1);
            testObserver.ErrorCount.ShouldBe(0);
            resource.IsDisposed.ShouldBeTrue();
            BusObserver.ItemCount.ShouldBe(1);
            BusObserver.Items.Single().InnerException.ShouldBeOfType<InvalidOperationException>();
        }

        [Test]
        public async Task UsingItemResilient_Handles_Async_Stream_Exception() {
            // ARRANGE
            var resource = new TestResource();
            var resourceFactory = new Func<TestResource>(() => resource);
            var observableFactory = new Func<TestResource, IObservable<int>>(_ => 
                Observable.Timer(10.Milliseconds()).SelectMany(_ => Observable.Throw<int>(new Exception("Async failure.")))
            );

            // ACT
            var testObserver = this.UsingItemResilient(resourceFactory, observableFactory, ["AsyncFail"])
                .PublishFaults()
                .Test();

            await testObserver.AwaitDoneAsync(100.Milliseconds());

            // ASSERT
            testObserver.CompletionCount.ShouldBe(1);
            testObserver.ErrorCount.ShouldBe(0);
            resource.IsDisposed.ShouldBeTrue();
            BusObserver.ItemCount.ShouldBe(1);
            BusObserver.Items.Single().InnerException.ShouldBeOfType<Exception>();
        }

        [Test]
        public void UsingItemResilient_Handles_Dispose_Exception() {
            // ARRANGE
            var resource = new TestResource { OnDispose = () => throw new InvalidOperationException("Dispose failed.") };
            var resourceFactory = new Func<TestResource>(() => resource);
            var observableFactory = new Func<TestResource, IObservable<int>>(_ => Observable.Return(42));

            // ACT
            var testObserver = this.UsingItemResilient(resourceFactory, observableFactory, ["DisposeFail"])
                .PublishFaults()
                .Test();

            // ASSERT
            testObserver.Items.ShouldBe(new[] { 42 });
            testObserver.CompletionCount.ShouldBe(1);
            testObserver.ErrorCount.ShouldBe(0);
            resource.IsDisposed.ShouldBeTrue();
            BusObserver.ItemCount.ShouldBe(1);
            BusObserver.Items.Single().InnerException.ShouldBeOfType<InvalidOperationException>();
        }
    }
 }
