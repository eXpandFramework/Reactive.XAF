using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.ErrorHandling;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Tests.Reactive.ProofOfConcepts {
    [TestFixture]
    public class FlowContextTests {
        private static readonly AsyncLocal<string> TestContext = new();

        [Test]
        public void FlowFaultContext_Preserves_Context_For_OnNext() {
            var source = new Subject<string>();
            string contextOnNext = null;
            const string expectedContext = "CONTEXT_ON_SUBSCRIBE";

            TestContext.Value = expectedContext;

            using var subscription = source
                .FlowContext(context:TestContext.Wrap())
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
                .FlowContext(context:TestContext.Wrap())
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
                .FlowContext(context:TestContext.Wrap())
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
        public void FlowContext_Preserves_Context_Across_Retry_Operator() {
            var subscriptionCount = 0;
            var source = InnerObservableWithContextAndError(() => subscriptionCount++);
            
            var stream = source
                .FlowContext(bus =>bus.Retry(3),TestContext.Wrap() )
                .CompleteOnError()
                
                ;
            
            stream.Subscribe();
            
            subscriptionCount.ShouldBe(3);
            
            TestContext.Value.ShouldBe("INNER_CONTEXT");
        }
    }    }
