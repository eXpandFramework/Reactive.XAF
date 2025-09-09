using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.Extensions.Tests {


    [TestFixture]
    public class SelectManySequentialTests {
        private ConcurrentDictionary<int, ISubject<Func<IObservable<Unit>>>> _sequencerScope;

        [SetUp]
        public void SetUp() {
            _sequencerScope = new ConcurrentDictionary<int, ISubject<Func<IObservable<Unit>>>>();
        }

        private IObservable<T> CreateRealTimeAction<T>(T value, TimeSpan duration)
            => Observable.Return(value).Delay(duration);

        [Test]
        public async Task Operations_With_Same_Key_Are_Executed_Sequentially() {
            var key = 1;
            var action1Duration = TimeSpan.FromMilliseconds(100);
            var action2Duration = TimeSpan.FromMilliseconds(150);
            var totalExpectedDuration = action1Duration + action2Duration;

            var action1 = CreateRealTimeAction("Result1", action1Duration);
            var action2 = CreateRealTimeAction("Result2", action2Duration);

            var stopwatch = new Stopwatch();

            stopwatch.Start();

            var task1 = "source1".SelectManySequential(() => action1, _ => key, _sequencerScope).ToTask();
            var task2 = "source2".SelectManySequential(() => action2, _ => key, _sequencerScope).ToTask();

            await Task.WhenAll(task1, task2);
            stopwatch.Stop();

            var result1 = await task1;
            var result2 = await task2;

            result1.ShouldBe("Result1");
            result2.ShouldBe("Result2");
            stopwatch.Elapsed.ShouldBeGreaterThanOrEqualTo(totalExpectedDuration);
        }

        [Test]
        public async Task Operations_With_Different_Keys_Are_Executed_In_Parallel() {
            var key1 = 1;
            var key2 = 2;
            var action1Duration = TimeSpan.FromMilliseconds(100);
            var action2Duration = TimeSpan.FromMilliseconds(150);
            var totalExpectedDuration = action1Duration + action2Duration;
            var longestDuration = action2Duration;

            var action1 = CreateRealTimeAction("Result1", action1Duration);
            var action2 = CreateRealTimeAction("Result2", action2Duration);

            var stopwatch = new Stopwatch();

            stopwatch.Start();

            var task1 = "source1".SelectManySequential(() => action1, _ => key1, _sequencerScope).ToTask();
            var task2 = "source2".SelectManySequential(() => action2, _ => key2, _sequencerScope).ToTask();

            await Task.WhenAll(task1, task2);
            stopwatch.Stop();

            var result1 = await task1;
            var result2 = await task2;

            result1.ShouldBe("Result1");
            result2.ShouldBe("Result2");
            stopwatch.Elapsed.ShouldBeGreaterThanOrEqualTo(longestDuration);
            stopwatch.Elapsed.ShouldBeLessThan(totalExpectedDuration);
        }

        [Test]
        public async Task Error_In_Action_Does_Not_Break_Sequencer_For_Subsequent_Operations() {
            var key = 1;
            var testException = new InvalidOperationException("Test Error");

            var failingAction = Observable.Throw<string>(testException);
            var succeedingAction = CreateRealTimeAction("Success", TimeSpan.FromMilliseconds(50));

            var failingTask = "source1".SelectManySequential(() => failingAction, _ => key, _sequencerScope).ToTask();
            await Should.ThrowAsync<InvalidOperationException>(() => failingTask);

            var succeedingTask = "source2".SelectManySequential(() => succeedingAction, _ => key, _sequencerScope).ToTask();
            var result = await succeedingTask;

            result.ShouldBe("Success");
        }

        [Test]
        [SuppressMessage("ReSharper", "MethodSupportsCancellation")]
        public async Task Cancellation_While_Waiting_Prevents_Action_From_Running() {
            var key = 1;
            var action1Duration = TimeSpan.FromMilliseconds(200);
            var action2Started = false;

            var action1 = CreateRealTimeAction("Done1", action1Duration);
            var action2 = Observable.Defer(() => {
                action2Started = true;
                return Observable.Return("Done2");
            });

            var cts = new CancellationTokenSource();

            var task1 = "source1".SelectManySequential(() => action1, _ => key, _sequencerScope).ToTask();
            var task2 = "source2".SelectManySequential(() => action2, _ => key, _sequencerScope).ToTask(cts.Token);

            await Task.Delay(50);

            await cts.CancelAsync();

            await task1;

            await Should.ThrowAsync<TaskCanceledException>(() => task2);

            action2Started.ShouldBeFalse();
        }
    }
}