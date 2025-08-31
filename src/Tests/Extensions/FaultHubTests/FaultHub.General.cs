using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.ExceptionExtensions;
using Xpand.Extensions.Numeric;
using Xpand.Extensions.Reactive.ErrorHandling;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Tests.FaultHubTests {
[TestFixture]
public class FaultHub_General : FaultHubTestBase {
    [Test]
    public async Task FaultHub_Context_Flows_Across_Schedulers() {
        var asyncStream = Observable.Throw<Unit>(new InvalidOperationException("Async Error"))
            .SubscribeOn(TaskPoolScheduler.Default);
            
        var streamWithContext = asyncStream.ChainFaultContext(["MainThreadContext"]);
            
        await streamWithContext.PublishFaults().Capture();
            
        BusEvents.Count.ShouldBe(1);

        var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
        fault.Context.UserContext.ShouldContain("MainThreadContext");
        fault.InnerException.ShouldBeOfType<InvalidOperationException>();
    }
        
    [Test]
    public async Task FaultHub_Context_Is_Isolated_In_Concurrent_Operations() {
        var streamA = Observable.Throw<Unit>(new InvalidOperationException("Error A"));
        var streamB = Observable.Throw<Unit>(new InvalidOperationException("Error B"));
        var resilientStreamA = streamA.ChainFaultContext(["ContextA"]).PublishFaults();
        var resilientStreamB = streamB.ChainFaultContext(["ContextB"]).PublishFaults();

        var mergedStream = resilientStreamA.Merge(resilientStreamB);
            
        await mergedStream.Capture();
            
        BusEvents.Count.ShouldBe(2);

        var faults = BusEvents.OfType<FaultHubException>().ToArray();
        faults.Length.ShouldBe(2);
            
        faults.SelectMany(f => f.Context.UserContext).ShouldContain("ContextA");
        faults.SelectMany(f => f.Context.UserContext).ShouldContain("ContextB");
    }
        
    [Test]
    public async Task FaultHub_Context_Is_Preserved_During_Async_Retries() {
        var attemptCount = 0;
        var failingStream = Observable.Defer(() => {
            attemptCount++;
            return Observable.Throw<Unit>(new InvalidOperationException("Retryable Error"));
        });
            
        var streamWithContext = failingStream.ChainFaultContext(source=>source.RetryWithBackoff(3, _ => 10.Milliseconds()), ["AsyncRetryContext"]);
            
        await streamWithContext.PublishFaults().Capture();
            
        attemptCount.ShouldBe(3);
            
        BusEvents.Count.ShouldBe(1);
            
        var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
        fault.Context.UserContext.ShouldContain("AsyncRetryContext");
        fault.InnerException.ShouldBeOfType<InvalidOperationException>();
    }

    [Test]
    public async Task Preserves_Origin_StackTrace_For_Asynchronous_Exception_Without_StackTrace() {
        var source = Observable.Timer(TimeSpan.FromMilliseconds(20))
            .SelectMany(_ => Observable.Throw<Unit>(new InvalidOperationException("Async stackless fail")));
            
        await source.ContinueOnFault().Capture();
            
        BusEvents.Count.ShouldBe(1);
        var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
        
        fault.LogicalStackTrace.ShouldContain(f => f.MemberName == nameof(Preserves_Origin_StackTrace_For_Asynchronous_Exception_Without_StackTrace));
    }
    
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
                .ContinueOnFault(context:["SuppressedContext"]) 
                .RethrowOnFault();

            var result = await stream.Capture();
            
            result.Error.ShouldNotBeNull();
            result.IsCompleted.ShouldBeFalse();
            result.Error.ShouldBeOfType<FaultHubException>();
            
            BusEvents.ShouldBeEmpty();
        }
        
    
        

}}