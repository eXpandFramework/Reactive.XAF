using System;
using System.Linq;
using System.Reactive.Linq;
using akarnokd.reactive_extensions;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Numeric;
using Xpand.Extensions.Reactive.Channels;
using Xpand.Extensions.Tests.FaultHubTests;

namespace Xpand.Extensions.Tests {
    [TestFixture]
    public class RpcChannelSuppressWithContextTests : FaultHubTestBase {

        [Test]
        public void SuppressWithContext_Does_Not_Suppress_By_Default() {
            var key = nameof(SuppressWithContext_Does_Not_Suppress_By_Default);
            var context = "Context";
            var item = "Item";

            var observer = Observable.Return(item)
                .SuppressWithContext(context, key)
                .Test();

            observer.AwaitDone(1.Seconds());
            observer.ItemCount.ShouldBe(1);
            observer.Items.Single().ShouldBe(item);
        }

        [Test]
        public void SuppressWithContext_Suppress_When_Handler_Returns_True() {
            var key = nameof(SuppressWithContext_Suppress_When_Handler_Returns_True);
            var context = 100;

            using var handler = key.SuppressWithContext<int>(ctx => ctx > 50)
                .Subscribe();

            var observer = Observable.Return("HideMe")
                .SuppressWithContext(context, key)
                .Test();

            observer.AwaitDone(1.Seconds());
            observer.ItemCount.ShouldBe(0);
        }

        [Test]
        public void SuppressWithContext_Builder_Syntax_Works() {
            var key = nameof(SuppressWithContext_Builder_Syntax_Works);
            var context = "Driver";

            using var handler = key.Suppress<string>()
                .Using(ctx => ctx == "Driver")
                .Subscribe();

            var observer = Observable.Return("Item")
                .SuppressWithContext(context, key)
                .Test();

            observer.AwaitDone(1.Seconds());
            observer.ItemCount.ShouldBe(0);
        }

        [Test]
        public void SuppressWithContext_Handles_Tuples() {
            var key = nameof(SuppressWithContext_Handles_Tuples);
            var context = (Active: true, Count: 5);

            using var handler = key.Suppress<(bool Active, int Count)>()
                .Using(ctx => ctx.Active)
                .Subscribe();

            var observer = Observable.Return("Item")
                .SuppressWithContext(context, key)
                .Test();

            observer.AwaitDone(1.Seconds());
            observer.ItemCount.ShouldBe(0);
        }
    }
}