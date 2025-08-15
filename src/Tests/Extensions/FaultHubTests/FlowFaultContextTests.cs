// Please add this new file to your Xpand.Extensions.Tests/FaultHubTests/ folder.
using System;
using System.Reactive.Subjects;
using System.Threading;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;

namespace Xpand.Extensions.Tests.FaultHubTests {
    [TestFixture]
    public class FlowFaultContextTests {
        private static readonly AsyncLocal<string> TestContext = new();

        [Test]
        public void FlowFaultContext_Preserves_Context_For_OnNext() {
            // ARRANGE
            var source = new Subject<string>();
            string contextOnNext = null;
            const string expectedContext = "CONTEXT_ON_SUBSCRIBE";

            TestContext.Value = expectedContext;

            using var subscription = source
                .FlowFaultContext()
                .Subscribe(
                    onNext: _ => contextOnNext = TestContext.Value,
                    onError: _ => { },
                    onCompleted: () => { }
                );
            
            // ACT
            TestContext.Value = "CONTEXT_ON_FIRE";
            source.OnNext("test");

            // ASSERT
            contextOnNext.ShouldBe(expectedContext);
        }

        [Test]
        public void FlowFaultContext_Preserves_Context_For_OnError() {
            // ARRANGE
            var source = new Subject<string>();
            string contextOnError = null;
            const string expectedContext = "CONTEXT_ON_SUBSCRIBE";
            
            TestContext.Value = expectedContext;
            
            using var subscription = source
                .FlowFaultContext()
                .Subscribe(
                    onNext: _ => { },
                    onError: _ => contextOnError = TestContext.Value,
                    onCompleted: () => { }
                );
            
            // ACT
            TestContext.Value = "CONTEXT_ON_FIRE";
            source.OnError(new Exception("test error"));

            // ASSERT
            contextOnError.ShouldBe(expectedContext);
        }

        [Test]
        public void FlowFaultContext_Preserves_Context_For_OnCompleted() {
            // ARRANGE
            var source = new Subject<string>();
            string contextOnCompleted = null;
            const string expectedContext = "CONTEXT_ON_SUBSCRIBE";

            TestContext.Value = expectedContext;

            using var subscription = source
                .FlowFaultContext()
                .Subscribe(
                    onNext: _ => { },
                    onError: _ => { },
                    onCompleted: () => contextOnCompleted = TestContext.Value
                );
            
            // ACT
            TestContext.Value = "CONTEXT_ON_FIRE";
            source.OnCompleted();

            // ASSERT
            contextOnCompleted.ShouldBe(expectedContext);
        }

    }
}