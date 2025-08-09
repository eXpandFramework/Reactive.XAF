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
using Xpand.Extensions.Numeric;
using Xpand.Extensions.Reactive.ErrorHandling;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.TestsLib.Common;

namespace Xpand.Extensions.Tests.FaultHubTests{
    public class OperatorFaultTests:FaultHubTestBase {
        [Test]
        public void SelectItemResilient_Continues_Stream_After_Error_And_Filters_Failing_Item() {

            var source = Enumerable.Range(1, 4).ToObservable();

            var testObserver = source.SelectItemResilient(num => {
                    if (num == 2) {
                        throw new InvalidOperationException("This is a test error.");
                    }

                    return num * 10;
                })
                .PublishFaults()
                .Test();

            testObserver.Items.ShouldBe(new[] { 10, 30, 40 });
            testObserver.ErrorCount.ShouldBe(0);
            testObserver.CompletionCount.ShouldBe(1);


            BusObserver.ItemCount.ShouldBe(1);
            BusObserver.Items.Single().InnerException.ShouldBeOfType<InvalidOperationException>();
        }

        [Test]
        public void CatchAndComplete_Suppresses_Error_Publishes_And_Completes() {
            var source = Observable.Throw<int>(new InvalidOperationException("Test Failure"));
            
            var testObserver = source.CatchAndCompleteOnFault(["TestContext"]).Test();
            
            testObserver.ItemCount.ShouldBe(0);
            testObserver.ErrorCount.ShouldBe(0);
            testObserver.CompletionCount.ShouldBe(1);

            BusObserver.ItemCount.ShouldBe(1);
            var fault = BusObserver.Items.Single().ShouldBeOfType<FaultHubException>();
            fault.InnerException.ShouldBeOfType<InvalidOperationException>();
            fault.Context.CustomContext.ShouldContain("TestContext");
        }

        [Test]
        public void CatchAndComplete_Provides_Item_Resilience_In_SelectMany() {
            var source = Observable.Range(1, 3);
            
            var testObserver = source.SelectMany(i => {
                if (i == 2) {
                    return Observable.Throw<int>(new InvalidOperationException("Failure on item 2"))
                        .CatchAndCompleteOnFault(["FailingItem"]);
                }
                return Observable.Return(i * 10);
            }).Test();
            
            testObserver.Items.ShouldBe(new[] { 10, 30 });
            testObserver.ErrorCount.ShouldBe(0);
            testObserver.CompletionCount.ShouldBe(1);

            BusObserver.ItemCount.ShouldBe(1);
            var fault = BusObserver.Items.Single().ShouldBeOfType<FaultHubException>();
            fault.InnerException.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Failure on item 2");
            fault.Context.CustomContext.ShouldContain("FailingItem");
        }
        
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
        public void SelectManySequentialItemResilient_survives_error() {
            using var testObserver = new TestObserver<Unit>();
            using var observer = 1.Range(3).ToNowObservable()
                .Do(_ => testObserver.OnNext(Unit.Default))
                .SelectManySequentialItemResilient(_ => Observable.Throw<int>(new Exception()))
                .PublishFaults().Test();

            observer.ErrorCount.ShouldBe(0);
            testObserver.ItemCount.ShouldBe(3);
            BusObserver.ItemCount.ShouldBe(3);
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
            var resource = new TestResource { OnDispose = () => throw new InvalidOperationException("Dispose failed.") };
            var factory = new Func<IObservable<int>>(() => Observable.Using(() => resource, _ => Observable.Return(42)));
            
            var testObserver = this.DeferItemResilient(factory, ["DisposeFail"]).Test();
            
            testObserver.Items.ShouldBe(new[] { 42 });
            testObserver.CompletionCount.ShouldBe(1);
            testObserver.ErrorCount.ShouldBe(0); // No unhandled error.
            
            resource.IsDisposed.ShouldBeTrue();


            BusObserver.ItemCount.ShouldBe(1);
            var fault = BusObserver.Items.Single().ShouldBeOfType<FaultHubException>();
            fault.InnerException.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Dispose failed.");
            fault.Context.CustomContext.ShouldContain("DisposeFail");
        }
        
        
        
        [Test]
        public void SelectManySequential_op(){
            var innerOpObserver = new TestObserver<int>();
            
            var opBus = Observable.Range(0,2)
                .SelectManySequential(_ => Observable.Defer(() => {
                    innerOpObserver.OnNext(1);
                    return Observable.Defer(() => Observable.Throw<Unit>(new Exception())).ChainFaultContext();
                }).PublishOnFault()) ;

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
                }).ChainFaultContext(retrySelector).PublishOnFault())
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
            var resourceFactory = new Func<TestResource>(() => throw new InvalidOperationException("Resource factory failed."));
            var observableFactory = new Func<TestResource, IObservable<int>>(_ => Observable.Return(42));
            
            var testObserver = this.UsingItemResilient(resourceFactory, observableFactory, ["ResourceFail"])
                .PublishFaults()
                .Test();
            
            testObserver.CompletionCount.ShouldBe(1);
            testObserver.ErrorCount.ShouldBe(0);
            BusObserver.ItemCount.ShouldBe(1);
            BusObserver.Items.Single().InnerException.ShouldBeOfType<InvalidOperationException>();
        }

        [Test]
        public void UsingItemResilient_Handles_ObservableFactory_Exception() {
            var resource = new TestResource();
            var resourceFactory = new Func<TestResource>(() => resource);
            var observableFactory = new Func<TestResource, IObservable<int>>(_ => throw new InvalidOperationException("Observable factory failed."));
            
            var testObserver = this.UsingItemResilient(resourceFactory, observableFactory, ["ObservableFail"])
                .PublishFaults()
                .Test();
            
            testObserver.CompletionCount.ShouldBe(1);
            testObserver.ErrorCount.ShouldBe(0);
            resource.IsDisposed.ShouldBeTrue();
            BusObserver.ItemCount.ShouldBe(1);
            BusObserver.Items.Single().InnerException.ShouldBeOfType<InvalidOperationException>();
        }

        [Test]
        public async Task UsingItemResilient_Handles_Async_Stream_Exception() {
            var resource = new TestResource();
            var resourceFactory = new Func<TestResource>(() => resource);
            var observableFactory = new Func<TestResource, IObservable<int>>(_ => 
                Observable.Timer(10.Milliseconds()).SelectMany(_ => Observable.Throw<int>(new Exception("Async failure.")))
            );
            
            var testObserver = this.UsingItemResilient(resourceFactory, observableFactory, ["AsyncFail"])
                .PublishFaults()
                .Test();

            await testObserver.AwaitDoneAsync(100.Milliseconds());
            
            testObserver.CompletionCount.ShouldBe(1);
            testObserver.ErrorCount.ShouldBe(0);
            resource.IsDisposed.ShouldBeTrue();
            BusObserver.ItemCount.ShouldBe(1);
            BusObserver.Items.Single().InnerException.ShouldBeOfType<Exception>();
        }

        [Test]
        public void UsingItemResilient_Handles_Dispose_Exception() {
            var resource = new TestResource { OnDispose = () => throw new InvalidOperationException("Dispose failed.") };
            var resourceFactory = new Func<TestResource>(() => resource);
            var observableFactory = new Func<TestResource, IObservable<int>>(_ => Observable.Return(42));
            
            var testObserver = this.UsingItemResilient(resourceFactory, observableFactory, ["DisposeFail"])
                .PublishFaults()
                .Test();
            
            testObserver.Items.ShouldBe(new[] { 42 });
            testObserver.CompletionCount.ShouldBe(1);
            testObserver.ErrorCount.ShouldBe(0);
            resource.IsDisposed.ShouldBeTrue();
            BusObserver.ItemCount.ShouldBe(1);
            BusObserver.Items.Single().InnerException.ShouldBeOfType<InvalidOperationException>();
        }
        
        [Test]
        public void DeferItemResilient_WithRetry_AttemptsOperationMultipleTimes_ThenSuppresses() {
            var attemptCount = 0;
            var retryStrategy = (Func<IObservable<int>, IObservable<int>>)(source => source.Retry(3));

            var testObserver = this.DeferItemResilient(() => {
                    attemptCount++;
                    return Observable.Throw<int>(new InvalidOperationException("Transient Failure"));
                }, retryStrategy)
                .Test();

            testObserver.ItemCount.ShouldBe(0);
            testObserver.ErrorCount.ShouldBe(0);
            testObserver.CompletionCount.ShouldBe(1);
            
            attemptCount.ShouldBe(3);
            
            BusObserver.ItemCount.ShouldBe(1);
            BusObserver.Items.Single().InnerException.ShouldBeOfType<InvalidOperationException>();
        }

        [Test]
        public void SelectManyItemResilient_WithRetry_RetriesFailingItem_WithoutAffectingOthers() {
            var attemptCount = 0;
            var source = Observable.Range(1, 3);
            var retryStrategy = (Func<IObservable<int>, IObservable<int>>)(obs => obs.Retry(2));

            var testObserver = source.SelectManyItemResilient(i => {
                if (i == 2) {
                    return Observable.Defer(() => {
                        attemptCount++;
                        return Observable.Throw<int>(new InvalidOperationException("Failure on item 2"));
                    });
                }
                return Observable.Return(i * 10);
            }, retryStrategy).Test();

            testObserver.Items.ShouldBe(new[] { 10, 30 });
            testObserver.ErrorCount.ShouldBe(0);
            testObserver.CompletionCount.ShouldBe(1);

            attemptCount.ShouldBe(2);

            BusObserver.ItemCount.ShouldBe(1);
            BusObserver.Items.Single().InnerException.ShouldBeOfType<InvalidOperationException>();
        }
        
        [Test]
        public void DoItemResilient_WithRetry_RetriesAction_And_PassesItemThrough() {
            var attemptCount = 0;
            var source = Observable.Return(42);
            var retryStrategy = (Func<IObservable<int>, IObservable<int>>)(obs => obs.Retry(3));

            var testObserver = source.DoItemResilient(_ => {
                attemptCount++;
                if (attemptCount < 3) {
                    throw new InvalidOperationException("Transient Do Failure");
                }
            }, retryStrategy).Test();

            testObserver.Items.ShouldBe(new[] { 42 });
            testObserver.ErrorCount.ShouldBe(0);
            testObserver.CompletionCount.ShouldBe(1);

            attemptCount.ShouldBe(3);
            
            BusObserver.ItemCount.ShouldBe(0); // The final attempt succeeds, so no error is published.
        }

        [Test]
        public void SelectItemResilient_WithRetry_RetriesSelector_And_FiltersFailingItem() {
            var attemptCount = 0;
            var source = Observable.Range(1, 3);
            var retryStrategy = (Func<IObservable<int>, IObservable<int>>)(obs => obs.Retry(2));

            var testObserver = source.SelectItemResilient(i => {
                if (i == 2) {
                    attemptCount++;
                    throw new InvalidOperationException("Transient Select Failure");
                }
                return i * 10;
            }, retryStrategy).Test();

            testObserver.Items.ShouldBe(new[] { 10, 30 });
            testObserver.ErrorCount.ShouldBe(0);
            testObserver.CompletionCount.ShouldBe(1);

            attemptCount.ShouldBe(2);

            BusObserver.ItemCount.ShouldBe(1);
            BusObserver.Items.Single().InnerException.ShouldBeOfType<InvalidOperationException>();
        }

        [Test]
        public void UsingItemResilient_WithRetry_RetriesObservableFactory_ThenSuppresses() {
            var factoryAttemptCount = 0;
            var retryStrategy = (Func<IObservable<int>, IObservable<int>>)(obs => obs.Retry(3));
            var resource = new TestResource();

            var testObserver = this.UsingItemResilient(
                () => resource,
                _ => {
                    factoryAttemptCount++;
                    return Observable.Throw<int>(new InvalidOperationException("Transient Using Failure"));
                },
                retryStrategy
            ).Test();

            testObserver.ItemCount.ShouldBe(0);
            testObserver.ErrorCount.ShouldBe(0);
            testObserver.CompletionCount.ShouldBe(1);
            
            factoryAttemptCount.ShouldBe(3);
            resource.IsDisposed.ShouldBeTrue();
            
            BusObserver.ItemCount.ShouldBe(1);
            BusObserver.Items.Single().InnerException.ShouldBeOfType<InvalidOperationException>();
        }
    
    }
}
 
