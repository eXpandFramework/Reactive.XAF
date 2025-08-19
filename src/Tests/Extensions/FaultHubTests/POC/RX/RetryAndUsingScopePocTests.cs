using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using NUnit.Framework;
using Shouldly;
using Unit = System.Reactive.Unit;

namespace Xpand.Extensions.Tests.FaultHubTests.POC.RX {
    [TestFixture]
    public class RetryAndUsingScopePocTests {
        private static readonly AsyncLocal<string> TestContext = new();

        
        private static IObservable<Unit> InnerObservableWithContextAndError(Action subscriptionCounter) {
            return Observable.Defer(() => {
                subscriptionCounter();
                Console.WriteLine($"[PoC-Retry] Inner observable subscribed to. Count: {TestContext.Value}");
                return Observable.Using(
                    () => {
                        Console.WriteLine("[PoC-Retry] Inner scope entered. Setting context to 'RETRY_CONTEXT'.");
                        TestContext.Value = "RETRY_CONTEXT";
                        return Disposable.Create(() => {
                            Console.WriteLine("[PoC-Retry] Inner scope's Dispose action is running. Clearing context.");
                            TestContext.Value = null;
                        });
                    },
                    _ => {
                        Console.WriteLine("[PoC-Retry] Inner observable is about to throw an exception.");
                        return Observable.Throw<Unit>(new InvalidOperationException("Inner Failure"));
                    }
                );
            });
        }

        [Test]
        public void Retry_Disposes_Upstream_And_Loses_Context_Before_Final_Catch() {
            var capturedContext = "CONTEXT_NOT_SET";
            var subscriptionCount = 0;

            var stream = InnerObservableWithContextAndError(() => subscriptionCount++)
                .Retry(3)
                .Catch((Exception _) => {
                    Console.WriteLine("[PoC-Retry] Outer Catch block is executing. Capturing context.");
                    capturedContext = TestContext.Value;
                    return Observable.Empty<Unit>();
                });
            
            stream.Subscribe();
            
            subscriptionCount.ShouldBe(3);

            
            capturedContext.ShouldBeNull();
            Console.WriteLine(
                $"[PoC-Retry] Assertion successful: Captured context is '{capturedContext ?? "null"}'.");
        }
    }
}