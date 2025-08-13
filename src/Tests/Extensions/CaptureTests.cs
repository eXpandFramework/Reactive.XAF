using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Tests {
    [TestFixture]
    public class CaptureTests {
        [Test]
        public async Task Should_Capture_All_Items_And_Completion() {
            var source = Observable.Range(1, 5);

            var result = await source.Capture();

            result.ItemCount.ShouldBe(5);
            result.Items.ShouldBe([1, 2, 3, 4, 5]);
            result.IsCompleted.ShouldBeTrue();
            result.Error.ShouldBeNull();
        }

        [Test]
        public async Task Should_Capture_Error() {
            var exception = new InvalidOperationException("test");
            var source = Observable.Throw<int>(exception);

            var result = await source.Capture();

            result.ItemCount.ShouldBe(0);
            result.Items.ShouldBeEmpty();
            result.IsCompleted.ShouldBeFalse();
            result.Error.ShouldBe(exception);
        }

        [Test]
        public async Task Should_Handle_Empty_Stream() {
            var source = Observable.Empty<string>();

            var result = await source.Capture();

            result.ItemCount.ShouldBe(0);
            result.IsCompleted.ShouldBeTrue();
            result.Error.ShouldBeNull();
        }

        [Test]
        [Timeout(1000)]
        public async Task Should_Not_Complete_For_Never_Stream() {
            var source = Observable.Never<int>();

            var task =await source.Capture();

            task.IsCompleted.ShouldBeFalse();
        }

        [Test]
        public async Task Should_Be_Thread_Safe() {
            const int itemCount = 100_000;
            var source = Observable.Range(0, itemCount)
                .Select(i => i % 2 == 0
                    ? Observable.Return(i).ObserveOn(ThreadPoolScheduler.Instance)
                    : Observable.Return(i).ObserveOn(TaskPoolScheduler.Default))
                .Merge();

            var result = await source.Capture();

            result.ItemCount.ShouldBe(itemCount);
            result.IsCompleted.ShouldBeTrue();
            result.Error.ShouldBeNull();
        }

        
    }
}