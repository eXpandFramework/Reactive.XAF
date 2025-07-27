using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using akarnokd.reactive_extensions;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Numeric;
using Xpand.Extensions.Reactive.ErrorHandling;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Transform.System;
using Xpand.TestsLib;

namespace Xpand.Extensions.Tests.TransformTests{
    public class SelectManySequentialTests : BaseTest {
        [Test]
        public void SelectManySequential_Empty() {
            var finalized = false;
            using var testObserver = Observable.Empty<int>()
                .SelectManySequential(i => i.Observe())
                .Finally(() => finalized = true)
                .Test();

            finalized.ShouldBe(true);
            testObserver.CompletionCount.ShouldBe(1);
            testObserver.ItemCount.ShouldBe(0);
        }

        [Test]
        public void SelectManySequential_ProjectsAndConcatenates() {
            var finalized = false;
            using var testObserver = Enumerable.Range(1, 3).ToArray().ToNowObservable()
                .SelectManySequential(i => Observable.Range(1, i))
                .Finally(() => finalized = true)
                .Test();

            finalized.ShouldBe(true);
            testObserver.CompletionCount.ShouldBe(1);
            testObserver.ItemCount.ShouldBe(1 + 2 + 3); 
            testObserver.Items.ShouldBe(new[] { 1, 1, 2, 1, 2, 3 });
        }
        
        [Test]
        public async Task SelectManySequential_MaintainsOrderForAsyncOperations() {
            var finallySignal = new TaskCompletionSource<bool>();
            IList<string> items = null;

            var stream = Enumerable.Range(1, 2).ToObservable()
                .SelectManySequential(i => i == 1 ? 20.ToMilliseconds().Timer().Select(_ => "First") : "Second".Observe())
                .Finally(() => finallySignal.SetResult(true)); 
            
            await Task.WhenAll(stream.ToList().ForEachAsync(result => items = result), finallySignal.Task);
            
            finallySignal.Task.IsCompletedSuccessfully.ShouldBe(true);
            items.ShouldNotBeNull();
            items.Count.ShouldBe(2);
            items.ShouldBe(["First", "Second"]);
        }
        [Test]
        public void SelectManySequential_ErrorInInnerSequencePropagates() {
            using (FaultHub.AddHandler(_ => true)) {
                var finalized = false;
                var ex = new InvalidOperationException("Failure");
                Exception capturedError = null;

                var stream = Enumerable.Range(1, 3).ToObservable()
                    .SelectManySequential(i => i == 2 ? Observable.Throw<int>(ex) : i.Observe())
                    .Finally(() => finalized = true);

                try {
                    stream.Wait();
                }
                catch (Exception e) {
                    capturedError = e is FaultHubException fhx ? fhx.InnerException : e;
                }

                finalized.ShouldBe(true);
                capturedError.ShouldBe(ex);
            }
        }
        
        [Test]
        public void SelectManySequential_FailsWhenSelectorIsHot() {
            // --- Arrange ---
            var startTimes = new System.Collections.Concurrent.ConcurrentQueue<DateTime>();
            var operationDuration = TimeSpan.FromMilliseconds(150);

            // This factory is "hot". It immediately records the start time when it's *called*,
            // proving that the work is initiated before subscription.
            Func<int, IObservable<int>> hotObservableFactory = value =>
            {
                startTimes.Enqueue(DateTime.UtcNow);
                // The real "work" is delayed, but the factory call has already happened.
                return Observable.Timer(operationDuration).Select(_ => value);
            };

            var source = new[] { 1, 2, 3 }.ToObservable();

            // --- Act ---
            // This is the operator under test. No fix has been applied.
            source.SelectManySequential(hotObservableFactory).Wait();

            // --- Assert ---
            // If execution were sequential, the time difference between the start of the 1st
            // and 2nd operation would be at least the duration of one operation (~150ms).
            var timeDifference = startTimes.ElementAt(1) - startTimes.ElementAt(0);

            // This assertion is expected to FAIL.
            // The actual timeDifference will be very small (e.g., < 1ms),
            // proving the factory was called for all items concurrently at the start.
            timeDifference.TotalMilliseconds.ShouldBeGreaterThan(operationDuration.TotalMilliseconds);
        }
    }
}