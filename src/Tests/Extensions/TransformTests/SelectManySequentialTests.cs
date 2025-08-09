using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using akarnokd.reactive_extensions;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Numeric;
using Xpand.Extensions.Reactive.ErrorHandling;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Transform.System;
using Xpand.TestsLib;
using TestScheduler = Microsoft.Reactive.Testing.TestScheduler;

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
            using (FaultHub.AddHandler(_ => FaultAction.Complete)) {
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
        public void SelectManySequential_Executes_Operations_In_Sequence() {
            // ARRANGE
            var eventLog = new System.Collections.Concurrent.ConcurrentQueue<string>();
            var source = new[] { 1, 2, 3 }.ToObservable();
            var operationDelay = TimeSpan.FromMilliseconds(100);

            // This selector logs when an operation starts and when it ends.
            Func<int, IObservable<int>> selector = i => {
                eventLog.Enqueue($"Start {i}");
                return Observable.Timer(operationDelay)
                    .Select(_ => i)
                    .Do(_ => eventLog.Enqueue($"End {i}"));
            };

            // ACT
            // We use TestObserver to run the stream and wait for it to complete.
            var testObserver = source.SelectManySequential(selector).Test();
            testObserver.AwaitDone(TimeSpan.FromSeconds(5));

            // ASSERT
            // The final values should be correct.
            testObserver.AssertResult(1, 2, 3);

            // The event log must show that each operation started and ended
            // before the next one began. This proves sequential execution.
            eventLog.ShouldBe(new[] { 
                "Start 1", "End 1", 
                "Start 2", "End 2", 
                "Start 3", "End 3" 
            });
        }
        
    }
}