using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using akarnokd.reactive_extensions;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.Transform;
using Xpand.TestsLib;

namespace Xpand.Extensions.Tests.TransformTests {
    public class DoNotCompleteTests:BaseTest {
        [Test]
        public void DoNotComplete_Arrays() {
            using var testObserver = Enumerable.Range(0, 2).ToArray().ToNowObservable().DoNotComplete().Finally(() => {}).Test();
            
            testObserver.CompletionCount.ShouldBe(0);
            testObserver.ItemCount.ShouldBe(2);
        }
        [Test]
        public void DoNotComplete_Empty() {
            using var testObserver = Observable.Empty<Unit>().DoNotComplete().Finally(() => {}).Test();
            
            testObserver.CompletionCount.ShouldBe(0);
            testObserver.ItemCount.ShouldBe(0);
        }

    }
}