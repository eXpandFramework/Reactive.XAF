using System.Reactive.Linq;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.Combine;

namespace Xpand.Extensions.Tests {
    [TestFixture]
    public class SwitchIfEmptyTests {
        [Test]
        public void SwitchIfEmpty_SourceNotEmpty_ShouldNotSwitch() {
            var source = Observable.Range(1, 5);
            var switchTo=Observable.Defer(() => Observable.Range(10, 5));
            var result = source.SwitchIfEmpty(switchTo);
            result.ToEnumerable().ShouldBe([1, 2, 3, 4, 5]);
        }

        [Test]
        public void SwitchIfEmpty_SourceEmpty_ShouldSwitch() {
            var source = Observable.Empty<int>();
            var switchTo = Observable.Range(10, 5);
            var result = source.SwitchIfEmpty(switchTo);
            var enumerable = result.ToEnumerable();
            enumerable.ShouldBe([10, 11, 12, 13, 14]);
        }

        [Test]
        public void SwitchIfEmpty_SourceNotEmptyAndSwitchToEmpty_ShouldNotSwitch() {
            var source = Observable.Range(1, 5);
            var switchTo = Observable.Empty<int>();
            var result = source.SwitchIfEmpty(switchTo);
            result.ToEnumerable().ShouldBe([1, 2, 3, 4, 5]);
        }

        [Test]
        public void SwitchIfEmpty_SourceEmptyAndSwitchToEmpty_ShouldNotSwitch() {
            var source = Observable.Empty<int>();
            var switchTo = Observable.Empty<int>();
            var result = source.SwitchIfEmpty(switchTo);
            result.ToEnumerable().ShouldBeEmpty();
        }
    }
}