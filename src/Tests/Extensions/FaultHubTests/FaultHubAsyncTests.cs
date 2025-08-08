using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using akarnokd.reactive_extensions;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Numeric;
using Xpand.Extensions.Reactive.ErrorHandling;
using Xpand.TestsLib.Common;

namespace Xpand.Extensions.Tests.FaultHubTests {
    [TestFixture]
    public class FaultHubAsyncTests : FaultHubTestBase {
        [Test]
        public async Task FaultHub_Context_Flows_Across_Schedulers() {
            var asyncStream = Observable.Throw<Unit>(new InvalidOperationException("Async Error"))
                .SubscribeOn(TaskPoolScheduler.Default);
            
            var streamWithContext = asyncStream.ChainFaultContext(["MainThreadContext"]);
            
            var testObserver = streamWithContext.PublishFaults().Test();
            
            await testObserver.AwaitDoneAsync(1.ToSeconds());

            
            BusObserver.ItemCount.ShouldBe(1);

            var fault = BusObserver.Items.Single().ShouldBeOfType<FaultHubException>();
            fault.Context.CustomContext.ShouldContain("MainThreadContext");
            fault.InnerException.ShouldBeOfType<InvalidOperationException>();
        }
        
        [Test]
        public async Task FaultHub_Context_Is_Isolated_In_Concurrent_Operations() {
            var streamA = Observable.Throw<Unit>(new InvalidOperationException("Error A"));
            var streamB = Observable.Throw<Unit>(new InvalidOperationException("Error B"));
            var resilientStreamA = streamA.ChainFaultContext(["ContextA"]).PublishFaults();
            var resilientStreamB = streamB.ChainFaultContext(["ContextB"]).PublishFaults();


            var mergedStream = resilientStreamA.Merge(resilientStreamB);
            
            var testObserver = mergedStream.Test();
            await testObserver.AwaitDoneAsync(1.ToSeconds());
            
            BusObserver.ItemCount.ShouldBe(2);

            var faults = BusObserver.Items.OfType<FaultHubException>().ToArray();
            faults.Length.ShouldBe(2);
            
            faults.SelectMany(f => f.Context.CustomContext).ShouldContain("ContextA");
            faults.SelectMany(f => f.Context.CustomContext).ShouldContain("ContextB");
        }
        
        [Test]
        public async Task FaultHub_Context_Is_Preserved_During_Async_Retries() {
            var attemptCount = 0;
            var failingStream = Observable.Defer(() => {
                attemptCount++;
                return Observable.Throw<Unit>(new InvalidOperationException("Retryable Error"));
            });
            
            var streamWithContext = failingStream.ChainFaultContext(source=>source.RetryWithBackoff(3, _ => 10.Milliseconds()), ["AsyncRetryContext"]);
            
            var testObserver = streamWithContext.PublishFaults().Test();
            await testObserver.AwaitDoneAsync(1.ToSeconds());
            
            attemptCount.ShouldBe(3);
            
            BusObserver.ItemCount.ShouldBe(1);
            
            var fault = BusObserver.Items.Single().ShouldBeOfType<FaultHubException>();
            fault.Context.CustomContext.ShouldContain("AsyncRetryContext");
            fault.InnerException.ShouldBeOfType<InvalidOperationException>();
        }

        [Test]
        public void Preserves_Origin_StackTrace_For_Asynchronous_Exception_Without_StackTrace() {
            // ARRANGE
            var source = Observable.Timer(TimeSpan.FromMilliseconds(20))
                .SelectMany(_ => Observable.Throw<Unit>(new InvalidOperationException("Async stackless fail")));

            // ACT
            using var testObserver = source.ContinueOnError().Test();
            testObserver.AwaitDone(TimeSpan.FromSeconds(1));

            // ASSERT
            BusObserver.ItemCount.ShouldBe(1);
            var fault = BusObserver.Items.Single().ShouldBeOfType<FaultHubException>();

            // The new assertion: Verify the ToString() output.
            var output = fault.ToString();

            // 1. Check for the special header indicating the stack trace was substituted.
            output.ShouldContain("--- Stack Trace (from innermost fault context) ---");

            // 2. Check that the substituted stack trace contains the name of this test method,
            // proving that the correct InvocationStackTrace was captured and used.
            output.ShouldContain(nameof(Preserves_Origin_StackTrace_For_Asynchronous_Exception_Without_StackTrace));
        }
    }
    
}