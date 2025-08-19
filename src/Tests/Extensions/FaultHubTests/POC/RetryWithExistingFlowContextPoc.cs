using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.Utility;
using Unit = System.Reactive.Unit;

namespace Xpand.Extensions.Tests.FaultHubTests.POC {
    [TestFixture]
    public class RetryWithExistingFlowContextPoc {
        private static readonly AsyncLocal<string> TestContext = new();

        
        private static IObservable<Unit> InnerObservableWithContextAndError(Action subscriptionCounter,
            List<string> log) {
            return Observable.Defer(() => {
                subscriptionCounter();
                log.Add($"-- Attempt Start. Context before Using: '{TestContext.Value ?? "null"}'.");
                return Observable.Using(
                    () => {
                        log.Add("   [Using] Factory entered. Setting context to 'INNER_CONTEXT'.");
                        TestContext.Value = "INNER_CONTEXT";
                        return Disposable.Create(() => {
                            log.Add(
                                $"   [Using] Dispose action running. Context is '{TestContext.Value ?? "null"}'. Clearing context.");
                            TestContext.Value = null;
                        });
                    },
                    _ => {
                        log.Add("      [Using] Inner stream is about to throw.");
                        return Observable.Throw<Unit>(new InvalidOperationException("Inner Failure"));
                    }
                );
            });
        }

        [Test]
        public void Existing_FlowFaultContext_Does_Not_Preserve_Context_Across_Retry() {
            var executionLog = new List<string>();
            var subscriptionCount = 0;
            var capturedContext = "CONTEXT_NOT_SET";

            
            var source = InnerObservableWithContextAndError(() => subscriptionCount++, executionLog);

            
            var sourceWithContextFlow = source.FlowContext(context:TestContext.Wrap());

            var stream = sourceWithContextFlow
                .Retry(3)
                .Catch((Exception ex) => {
                    executionLog.Add($"--- OuterCatch Executing. Final context is '{TestContext.Value ?? "null"}'.");
                    capturedContext = TestContext.Value;
                    return Observable.Empty<Unit>();
                });

            
            stream.Subscribe();

            
            Console.WriteLine("--- Execution Log ---");
            Console.WriteLine(string.Join(Environment.NewLine, executionLog));
            Console.WriteLine("---------------------");

            
            
            capturedContext.ShouldBeNull();
            subscriptionCount.ShouldBe(3);
        }
    }
}