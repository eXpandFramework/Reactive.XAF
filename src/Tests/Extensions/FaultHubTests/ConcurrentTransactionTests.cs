using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Numeric;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Tests.FaultHubTests {
    [TestFixture]
    public class ConcurrentTransactionTests : FaultHubTestBase {
        [Test]
        public async Task ConcurrentTransaction_RunToCompletion_Executes_All_And_Aggregates_Failures() {
            var stopwatch = Stopwatch.StartNew();

            var result = await Observable.Timer(100.Milliseconds()).Select(_ => "Success 1")
                .BeginBatchTransaction()
                .Add(Observable.Timer(150.Milliseconds()).SelectMany(_ => Observable.Throw<string>(new InvalidOperationException("Failure 1"))))
                .Add(Observable.Timer(50.Milliseconds()).Select(_ => "Success 2"))
                .Add(Observable.Timer(200.Milliseconds()).SelectMany(_ => Observable.Throw<string>(new InvalidOperationException("Failure 2"))))
                .ConcurrentTransaction("Concurrent-Tx")
                .PublishFaults()
                .Capture();

            stopwatch.Stop();

            stopwatch.ElapsedMilliseconds.ShouldBeLessThan(350);

            result.IsCompleted.ShouldBe(true);
            result.Items.Count.ShouldBe(2);
            result.Items.Cast<string>().ShouldContain("Success 1");
            result.Items.Cast<string>().ShouldContain("Success 2");

            BusEvents.Count.ShouldBe(1);
            var finalFault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            finalFault.AllContexts.ShouldContain("Concurrent-Tx");

            var aggregate = finalFault.InnerException.ShouldBeOfType<AggregateException>();
            aggregate.InnerExceptions.Count.ShouldBe(2);

            var failure1 = aggregate.InnerExceptions.OfType<FaultHubException>()
                .FirstOrDefault(ex => ex.AllContexts.Contains("Concurrent-Tx - Observable.Timer(150.Milliseconds()).SelectMany(_ => Observable.Throw<string>(new InvalidOperationException(\"Failure 1\")))"));
            failure1.ShouldNotBeNull();

            var failure2 = aggregate.InnerExceptions.OfType<FaultHubException>()
                .FirstOrDefault(ex => ex.AllContexts.Contains("Concurrent-Tx - Observable.Timer(200.Milliseconds()).SelectMany(_ => Observable.Throw<string>(new InvalidOperationException(\"Failure 2\")))"));
            failure2.ShouldNotBeNull();
        }
        [Test]
        public async Task ConcurrentTransaction_FailFast_Terminates_On_First_Error() {
            var stopwatch = Stopwatch.StartNew();
            var slowerOperationCompleted = false;

            var result = await Observable.Timer(200.Milliseconds()).Select(_ => "Success - Should be cancelled").Do(_ => slowerOperationCompleted = true)
                .BeginBatchTransaction()
                .Add(Observable.Timer(50.Milliseconds()).SelectMany(_ => Observable.Throw<string>(new InvalidOperationException("Fast Failure"))))
                .ConcurrentTransaction("Concurrent-FailFast-Tx", failFast: true)
                .Capture();

            stopwatch.Stop();

            stopwatch.ElapsedMilliseconds.ShouldBeLessThan(150);
            slowerOperationCompleted.ShouldBeFalse("The slower operation should have been cancelled.");

            result.IsCompleted.ShouldBe(false);
            result.Error.ShouldNotBeNull();

            var outerTxException = result.Error.ShouldBeOfType<InvalidOperationException>();
            outerTxException.Message.ShouldBe("Concurrent-FailFast-Tx failed");

            var innerFault = outerTxException.InnerException.ShouldBeOfType<FaultHubException>();

            innerFault.InnerException.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Fast Failure");

            innerFault.AllContexts.ShouldContain("Concurrent-FailFast-Tx - Observable.Timer(50.Milliseconds()).SelectMany(_ => Observable.Throw<string>(new InvalidOperationException(\"Fast Failure\")))");

            BusEvents.ShouldBeEmpty();
        }        
        [Test]
        public async Task ConcurrentTransaction_Succeeds_When_All_Operations_Succeed() {
            var stopwatch = Stopwatch.StartNew();

            var result = await Observable.Timer(150.Milliseconds()).Select(_ => "Op 1")
                .BeginBatchTransaction()
                .Add(Observable.Timer(50.Milliseconds()).Select(_ => "Op 2"))
                .Add(Observable.Timer(100.Milliseconds()).Select(_ => "Op 3"))
                .ConcurrentTransaction("Concurrent-Success-Tx")
                .PublishFaults()
                .Capture();

            stopwatch.Stop();

            stopwatch.ElapsedMilliseconds.ShouldBeInRange(140, 250);

            result.IsCompleted.ShouldBe(true);
            result.Error.ShouldBeNull();
            result.Items.Count.ShouldBe(3);

            result.Items.Cast<string>().ShouldBe(["Op 2", "Op 3", "Op 1"]);

            BusEvents.ShouldBeEmpty();
        }
        [Test]
        public async Task ConcurrentTransaction_Obeys_MaxConcurrency() {
            var stopwatch = Stopwatch.StartNew();
            int activeOperations = 0;
            int maxObservedConcurrency = 0;
            var lockObject = new object();

            var operations = Enumerable.Range(1, 4).Select(i =>
                Observable.Defer(() => {
                    lock (lockObject) {
                        activeOperations++;
                        maxObservedConcurrency = Math.Max(maxObservedConcurrency, activeOperations);
                    }
                    return Observable.Timer(150.Milliseconds()).Select(_ => i);
                }).Finally(() => {
                    lock (lockObject) {
                        activeOperations--;
                    }
                })
            );

            var result = await operations
                .BeginBatchTransaction()
                .ConcurrentTransaction("Concurrent-Max-Tx", maxConcurrency: 2)
                .PublishFaults()
                .Capture();

            stopwatch.Stop();

            maxObservedConcurrency.ShouldBe(2);

            stopwatch.ElapsedMilliseconds.ShouldBeInRange(280, 450);

            result.IsCompleted.ShouldBe(true);
            result.Items.Count.ShouldBe(4);
            BusEvents.ShouldBeEmpty();
        }
        
        [Test]
        public async Task Can_Create_Concurrent_Transaction_From_IEnumerable_Of_Observables() {
            var items = new[] { "Success1", "Failure1", "Success2" };
            var operations = items.Select(item => {
                if (item.StartsWith("Failure")) {
                    return Observable.Throw<string>(new InvalidOperationException(item));
                }
                return Observable.Return(item);
            });

            var result = await operations
                .BeginBatchTransaction()
                .ConcurrentTransaction("FromIEnumerableConcurrentTx")
                .PublishFaults()
                .Capture();

            result.IsCompleted.ShouldBe(true);
            result.Items.Count.ShouldBe(2);
            result.Items.Cast<string>().ShouldContain("Success1");
            result.Items.Cast<string>().ShouldContain("Success2");

            BusEvents.Count.ShouldBe(1);
            var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
    
            var aggregate = fault.InnerException.ShouldBeOfType<AggregateException>();
            var innerFault = aggregate.InnerExceptions.Single().ShouldBeOfType<FaultHubException>();

            innerFault.InnerException.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Failure1");

            innerFault.AllContexts.ShouldContain("FromIEnumerableConcurrentTx - operations[1]");
        }
    }
}