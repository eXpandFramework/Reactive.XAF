using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Numeric;
using Xpand.Extensions.Reactive.ErrorHandling;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Tests.FaultHubTests;
[TestFixture]
public class FaultHubAsyncTests : FaultHubTestBase {
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
            
        var output = fault.ToString();
        
        output.ShouldContain(nameof(Preserves_Origin_StackTrace_For_Asynchronous_Exception_Without_StackTrace));
    }
}