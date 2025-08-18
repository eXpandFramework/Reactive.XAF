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
            var source = new Subject<string>();
            string contextOnNext = null;
            const string expectedContext = "CONTEXT_ON_SUBSCRIBE";

            TestContext.Value = expectedContext;

            using var subscription = source
                .FlowFaultContext(TestContext.Wrap())
                .Subscribe(
                    onNext: _ => contextOnNext = TestContext.Value,
                    onError: _ => { },
                    onCompleted: () => { }
                );
            
            TestContext.Value = "CONTEXT_ON_FIRE";
            source.OnNext("test");
            
            contextOnNext.ShouldBe(expectedContext);
        }

        [Test]
        public void FlowFaultContext_Preserves_Context_For_OnError() {
            var source = new Subject<string>();
            string contextOnError = null;
            const string expectedContext = "CONTEXT_ON_SUBSCRIBE";
            
            TestContext.Value = expectedContext;
            
            using var subscription = source
                .FlowFaultContext(TestContext.Wrap())
                .Subscribe(
                    onNext: _ => { },
                    onError: _ => contextOnError = TestContext.Value,
                    onCompleted: () => { }
                );
            
            TestContext.Value = "CONTEXT_ON_FIRE";
            source.OnError(new Exception("test error"));
            
            contextOnError.ShouldBe(expectedContext);
        }

        [Test]
        public void FlowFaultContext_Preserves_Context_For_OnCompleted() {
            var source = new Subject<string>();
            string contextOnCompleted = null;
            const string expectedContext = "CONTEXT_ON_SUBSCRIBE";

            TestContext.Value = expectedContext;

            using var subscription = source
                .FlowFaultContext(TestContext.Wrap())
                .Subscribe(
                    onNext: _ => { },
                    onError: _ => { },
                    onCompleted: () => contextOnCompleted = TestContext.Value
                );
            
            TestContext.Value = "CONTEXT_ON_FIRE";
            source.OnCompleted();
            
            contextOnCompleted.ShouldBe(expectedContext);
        }

    }
}