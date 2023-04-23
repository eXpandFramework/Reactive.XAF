using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using akarnokd.reactive_extensions;
using Humanizer;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Transform.System;
using Xpand.Extensions.Reactive.Transform.System.Net;
using Xpand.Extensions.Reactive.Transform.System.Text.Json;
using Xpand.Extensions.StringExtensions;

namespace Xpand.Extensions.Tests {
    public class JsonExtensionTests {
        [Test]
        public void ToJsonDocument_DisposesDocument_Selection() {
            using var stream = new MemoryStream("{\"name\": \"John Doe\", \"age\": 42}".Bytes());
            var observer = stream.ToJsonDocument(jsonDocument => 1.Seconds().Timer().Where(_ => !jsonDocument.IsDisposed()))
                .Where(t => !t.document.IsDisposed())
                .Test();

            observer.AwaitDone(TimeSpan.FromSeconds(2));
            observer.CompletionCount.ShouldBe(1);
            observer.ItemCount.ShouldBe(1);
            observer.Items.First().document.IsDisposed().ShouldBeTrue();

        }
        [Test]
        public void ToJsonDocument_DisposesDocument_() {
            using var stream = new MemoryStream("{\"name\": \"John Doe\", \"age\": 42}".Bytes());
            var observer = stream.ToJsonDocument(document => document.ReturnObservable()).SelectMany()
                .Where(t => !t.IsDisposed())
                .Test();

            observer.AwaitDone(TimeSpan.FromSeconds(2));
            observer.CompletionCount.ShouldBe(1);
            observer.ItemCount.ShouldBe(1);
            observer.Items.First().IsDisposed().ShouldBeTrue();

        }

    }
}