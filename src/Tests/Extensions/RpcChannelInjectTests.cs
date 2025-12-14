using System;
using System.Linq;
using System.Reactive.Linq;
using akarnokd.reactive_extensions;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Numeric;
using Xpand.Extensions.Reactive.Channels;
using Xpand.Extensions.Tests.FaultHubTests;

namespace Xpand.Extensions.Tests{
    [TestFixture]
    public class RpcChannelInjectTests : FaultHubTestBase {
        [Test]
        public void Inject_Operator_Passes_Through_When_No_Handler_Is_Present() {
            var key = "inject-no-handler";
            var item = "original";

            var observer = Observable.Return(item)
                .Inject(key)
                .Test();

            observer.AwaitDone(1.Seconds());
            observer.ItemCount.ShouldBe(1);
            observer.Items.Single().ShouldBe(item);
        }

        [Test]
        public void Inject_Operator_Replaces_Item_Using_Handler() {
            var key = "inject-replace";
            var item = 10;

            using var handler = key.Inject<int, string>(x => Observable.Return(x * 2))
                .Subscribe();

            var observer = Observable.Return(item)
                .Inject(key)
                .Test();

            observer.AwaitDone(1.Seconds());
            observer.Items.Single().ShouldBe(20);
        }

        [Test]
        public void Inject_Operator_Can_Suppress_Item_By_Returning_Empty() {
            var key = "inject-suppress";

            using var handler = key.Inject<string, string>(_ => Observable.Empty<string>())
                .Subscribe();

            var observer = Observable.Return("hide-me")
                .Inject(key)
                .Test();

            observer.AwaitDone(1.Seconds());
            observer.ItemCount.ShouldBe(0);
        }

        [Test]
        public void Inject_Operator_Can_Expand_Single_Item_To_Many() {
            var key = "inject-expand";

            using var handler = key.Inject<int, string>(x => Observable.Range(x, 3))
                .Subscribe();

            var observer = Observable.Return(10)
                .Inject(key)
                .Test();

            observer.AwaitDone(1.Seconds());
            observer.ItemCount.ShouldBe(3);
            observer.Items.ShouldBe(new[] { 10, 11, 12 });
        }
    }
}