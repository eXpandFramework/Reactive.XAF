using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Transform.System;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Tests {
    [TestFixture]
    public class TriggerSideEffectTests {
        [Test]
        public async Task Item_Is_Emitted_Immediately_And_Side_Effect_Is_Triggered() {
            var sideEffectStarted = false;
            var sideEffectSource = new Subject<int>();
            var source = new Subject<string>();

            var resultTask = source.TriggerSideEffect(_ => {
                sideEffectStarted = true;
                return sideEffectSource;
            }).Capture();

            sideEffectStarted.ShouldBeFalse("Side effect should not start before the source emits.");
        
            source.OnNext("A");
            source.OnCompleted();
            sideEffectSource.OnCompleted();
            
            var result = await resultTask;

            result.Items.ShouldHaveSingleItem();
            result.Items[0].ShouldBe("A");
            sideEffectStarted.ShouldBeTrue("Side effect should have been triggered after the source emitted.");        }


        [Test]
        public async Task Error_In_Side_Effect_Propagates_And_Terminates_Stream() {
            var testException = new InvalidOperationException("Side Effect Failed");
            var sideEffect = new Subject<int>();
            var source = new Subject<string>();

            var resultTask = source.TriggerSideEffect(_ => sideEffect).Capture();

            source.OnNext("A");
            sideEffect.OnError(testException);

            var result = await resultTask;
            result.Error.ShouldBe(testException);
            
        }

        
        [Test]
        public async Task Unsubscription_Cancels_In_Flight_Side_Effect() {
            var sideEffectStarted = new TaskCompletionSource<bool>();
            var sideEffectCompleted = new TaskCompletionSource<bool>();
            var source = new Subject<string>();

            var subscription = source
                .TriggerSideEffect(_ => Observable.Interval(TimeSpan.FromMilliseconds(100))
                    .Take(10) 
                    .DoOnSubscribe(() => sideEffectStarted.SetResult(true))
                    .DoOnComplete(() => sideEffectCompleted.TrySetResult(true))
                )
                .Subscribe();

            source.OnNext("A");

            await sideEffectStarted.Task;

            subscription.Dispose();

            var completedTask = await Task.WhenAny(sideEffectCompleted.Task, Task.Delay(200));

            completedTask.ShouldNotBe(sideEffectCompleted.Task, "The side effect should have been cancelled by the unsubscription and not completed.");
            
        }


        [Test]
        public async Task Works_Correctly_When_Side_Effect_Is_On_Different_Thread() {
            var source = new Subject<string>();
            var sideEffectChannel = new Subject<int>();
            var currentManagedThreadId = Environment.CurrentManagedThreadId;
            var resultTask = sideEffectChannel
                .Select(item => item)
                .Merge(source.TriggerSideEffect(_ => TimeSpan.Zero.Timer()
                    .Do(_ => sideEffectChannel.OnNext(Environment.CurrentManagedThreadId))).IgnoreElements().To<int>())
                .Take(1)
                .Capture();

            source.OnNext("A");

            var result = await resultTask;

            result.IsCompleted.ShouldBeTrue();
            result.Items.Count.ShouldBe(1);
            result.Items.First().ShouldNotBe(currentManagedThreadId, "Side effect should have run on a different thread.");        }
        
        
         [Test]
        public async Task Item_Is_Emitted_And_Side_Effect_Is_Triggered() {
            var sideEffectSubscribed = false;
            var source = Observable.Return("A");

            var result = await source.TriggerSideEffect(_ => Observable.Create<int>(_ => {
                sideEffectSubscribed = true;
                return Disposable.Empty;
            })).Capture();

            result.Items.ShouldHaveSingleItem().ShouldBe("A");
            sideEffectSubscribed.ShouldBeTrue();
        }

        [Test]
        public async Task Stream_Completes_When_Source_Completes_Even_If_Side_Effect_Does_Not() {
            var source = Observable.Return("A");
            var nonTerminatingSideEffect = Observable.Never<int>();

            var result = await source
                .TriggerSideEffect(_ => nonTerminatingSideEffect)
                .Capture();

            result.IsCompleted.ShouldBeTrue("The stream should complete when the source completes, regardless of the side effect.");
            result.Items.ShouldHaveSingleItem().ShouldBe("A");
        }

        [Test]
        public async Task Multiple_Items_Trigger_Multiple_Side_Effects() {
            var sideEffectSubscriptionCount = 0;
            var source = new[] { "A", "B", "C" }.ToObservable();

            await source.TriggerSideEffect(_ => Observable.Create<int>(_ => {
                sideEffectSubscriptionCount++;
                return Disposable.Empty;
            })).Capture();

            sideEffectSubscriptionCount.ShouldBe(3);
        }

        [Test]
        public async Task Error_In_Source_Propagates_Immediately() {
            var testException = new InvalidOperationException("Source Failed");
            var source = Observable.Throw<string>(testException);
            var sideEffectTriggered = false;

            var result = await source.TriggerSideEffect(_ => {
                sideEffectTriggered = true;
                return Observable.Never<int>();
            }).Capture();

            result.Error.ShouldBe(testException);
            sideEffectTriggered.ShouldBeFalse();
        }

        [Test]
        public async Task Error_In_Side_Effect_Propagates_To_Main_Stream() {
            var testException = new InvalidOperationException("Side Effect Failed");
            var source = new Subject<string>();

            var resultTask = source.TriggerSideEffect(_ => Observable.Throw<int>(testException))
                .Capture();

            source.OnNext("A");

            var result = await resultTask;
            result.Error.ShouldBe(testException);        }

        [Test]
        public async Task Error_In_Later_Side_Effect_Propagates_After_Earlier_Items_Emitted() {
            var testException = new InvalidOperationException("Side Effect B Failed");
            var source = new Subject<string>();
            var sideEffectA = new Subject<int>();
            var sideEffectB = new Subject<int>();

            var resultTask = source.TriggerSideEffect(item => item == "A" ? sideEffectA : sideEffectB).Capture();
            
            source.OnNext("A");
            source.OnNext("B");
            
            sideEffectB.OnError(testException);

            var result = await resultTask;

            result.Items.ShouldBe(["A", "B"]);
            result.Error.ShouldBe(testException);
        }

        [Test]
        public async Task Error_In_Handler_Factory_Function_Propagates_Immediately() {
            var testException = new InvalidOperationException("Handler factory failed");
            var source = Observable.Return("A");

            var result = await source.TriggerSideEffect<string, int>(_ => throw testException).Capture();

            result.Error.ShouldBe(testException);
        }

        [Test]
        public async Task Unsubscription_Disposes_Source_And_All_Active_Side_Effects() {
            var sourceDisposed = new TaskCompletionSource<bool>();
            var sideEffectADisposed = new TaskCompletionSource<bool>();
            var sideEffectBDisposed = new TaskCompletionSource<bool>();

            var sourceSubject = new Subject<string>();
            var sourceWithDisposalDetection = sourceSubject.Unsubscribed(() => sourceDisposed.SetResult(true));
        
            var subscription = sourceWithDisposalDetection.TriggerSideEffect(item => {
                var tcs = item == "A" ? sideEffectADisposed : sideEffectBDisposed;
                return Observable.Never<int>().Unsubscribed(() => tcs.SetResult(true));
            }).Subscribe();

            sourceSubject.OnNext("A");
            sourceSubject.OnNext("B");

            await Task.Delay(50);
            subscription.Dispose();

            var allDisposed = await Task.WhenAll(sourceDisposed.Task, sideEffectADisposed.Task, sideEffectBDisposed.Task)
                .ContinueWith(t => t.IsCompletedSuccessfully);
        
            allDisposed.ShouldBeTrue("Disposing the main subscription should dispose the source and all active side effects.");
            
        }

        [Test]
        public async Task Empty_Source_Stream_Completes_Immediately() {
            var sideEffectTriggered = false;
            
            var result = await Observable.Empty<string>()
                .TriggerSideEffect(_ => {
                    sideEffectTriggered = true;
                    return Observable.Never<int>();
                }).Capture();

            result.IsCompleted.ShouldBeTrue();
            result.Items.ShouldBeEmpty();
            sideEffectTriggered.ShouldBeFalse();
        }

        [Test]
        public async Task Side_Effect_Values_Are_Ignored() {
            var sideEffect = new Subject<int>();
            var source = new Subject<string>();

            var resultTask = source.TriggerSideEffect(_ => sideEffect).Capture();

            source.OnNext("A");
            sideEffect.OnNext(100);
            sideEffect.OnNext(200);
            
            source.OnCompleted();

            var result = await resultTask;

            result.Items.ShouldHaveSingleItem().ShouldBe("A");
            result.IsCompleted.ShouldBeTrue();
        }
    }
}