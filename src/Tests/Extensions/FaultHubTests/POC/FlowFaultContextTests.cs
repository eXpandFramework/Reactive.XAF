using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Tests.FaultHubTests.POC {
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

        
        private static IObservable<Unit> InnerObservableWithContextAndError(Action subscriptionCounter) {
            return Observable.Defer(() => {
                subscriptionCounter();
                return Observable.Using(
                    () => {
                        Console.WriteLine("[PoC] Inner scope entered. Setting context to 'INNER_CONTEXT'.");
                        TestContext.Value = "INNER_CONTEXT";
                        return Disposable.Create(() => {
                            Console.WriteLine("[PoC] Inner scope's Dispose action is running. Clearing context.");
                            TestContext.Value = null;
                        });
                    },
                    _ => Observable.Throw<Unit>(new InvalidOperationException("Inner Failure"))
                );
            });
        }


        [Test]
        public void FlowFaultContext_Preserves_Context_Across_Retry_Operator() {
            var capturedContext = "CONTEXT_NOT_SET";
            var subscriptionCount = 0;

            // Arrange
            var source = InnerObservableWithContextAndError(() => subscriptionCount++);

            // 1. Wrap the source with FlowFaultContext. This captures the context at the
            //    moment of each subscription and restores it for each notification.
            var sourceWithContextFlow = source.FlowFaultContext(TestContext.Wrap());

            var stream = sourceWithContextFlow
                // 2. Apply Retry. When Retry re-subscribes, it re-subscribes to sourceWithContextFlow.
                .Retry(3)
                // 3. The final Catch. It will execute inside the context restored by FlowFaultContext.
                .Catch((Exception _) => {
                    Console.WriteLine("[PoC] Outer Catch block is executing. Capturing context.");
                    capturedContext = TestContext.Value;
                    return Observable.Empty<Unit>();
                });

            // Act
            stream.Subscribe();

            // Assert
            subscriptionCount.ShouldBe(3);

            // The key assertion: The context set by the inner observable's Using block
            // should be preserved by FlowFaultContext and be visible here.
            capturedContext.ShouldBe("INNER_CONTEXT");
            Console.WriteLine($"[PoC] Assertion successful: Captured context is '{capturedContext}'.");
        }
    }
}