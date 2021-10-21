using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using akarnokd.reactive_extensions;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.Transform;
using Xpand.TestsLib;

namespace Xpand.Extensions.Tests {
    public class TransformTests:BaseTest {
        [Test]
        public void DoNotComplete_Arrays() {
            var testObserver = Enumerable.Range(0, 2).ToArray().ToNowObservable().DoNotComplete().Finally(() => {}).Test();
            
            testObserver.CompletionCount.ShouldBe(0);
            testObserver.ItemCount.ShouldBe(2);
        }
        [Test]
        public void DoNotComplete_Empty() {
            var testObserver = Observable.Empty<Unit>().DoNotComplete().Finally(() => {}).Test();
            
            testObserver.CompletionCount.ShouldBe(0);
            testObserver.ItemCount.ShouldBe(0);
        }

    }
}