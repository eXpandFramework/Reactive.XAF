using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using akarnokd.reactive_extensions;
using NUnit.Framework;
using Xpand.Extensions.Reactive.Utility;
using Xpand.TestsLib;

namespace Xpand.Extensions.Tests {
    public class ObserverLatestTests : BaseTest {
        [Test]
        public async Task ObserveLatestOn_WithScheduler_ShouldObserveLatestValueOnScheduler_EventLoop()
        {
            // Arrange
            var source = new Subject<int>();
            var result = new List<int>();
            var scheduler = new EventLoopScheduler();
            var completedSignal = new ManualResetEvent(false);

            // Act
            using (var subscription = source.ObserveLatestOn(scheduler).Subscribe(
                       onNext: value => result.Add(value),
                       onCompleted: () => completedSignal.Set()))
            {
                source.OnNext(1);
                source.OnNext(2);
                source.OnNext(3);

                // Wait for scheduler to process items
                await Task.Delay(100);

                // Assert
                CollectionAssert.AreEqual(new[] { 3 }, result, "Only the latest value (3) should be observed");

                source.OnNext(4);
                source.OnNext(5);

                // Wait for scheduler to process items
                await Task.Delay(100);

                // Assert
                CollectionAssert.AreEqual(new[] { 3, 5 }, result, "Latest values (3, 5) should be observed");

                // Complete the sequence
                source.OnCompleted();

                // Wait for completion signal
                completedSignal.WaitOne();
            }
        }
        [Test]
        public void ObserveLatestOn_WithScheduler_ShouldObserveLatestValueOnScheduler() {
            // Arrange
            var testScheduler = new TestScheduler();
            var source = new Subject<int>();
            var result = new List<int>();
            var scheduler = testScheduler;

            // Act
            using (var subscription = source.ObserveLatestOn(scheduler).Subscribe(result.Add)) {
                source.OnNext(1);
                source.OnNext(2);
                source.OnNext(3);

                // Assert
                CollectionAssert.IsEmpty(result, "No values should be observed before advancing the scheduler");

                testScheduler.AdvanceTimeBy(1);

                // Assert
                CollectionAssert.AreEqual(new[] { 3 }, result,
                    "Only the latest value (3) should be observed after advancing the scheduler");

                source.OnNext(4);
                source.OnNext(5);

                testScheduler.AdvanceTimeBy(1);

                // Assert
                CollectionAssert.AreEqual(new[] { 3, 5 }, result,
                    "Latest values (3, 5) should be observed after advancing the scheduler");
            }
        }

    
    }


}
