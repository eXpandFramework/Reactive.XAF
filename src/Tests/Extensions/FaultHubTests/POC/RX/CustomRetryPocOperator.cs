using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using NUnit.Framework;
using Shouldly;
using Unit = System.Reactive.Unit;

namespace Xpand.Extensions.Tests.FaultHubTests.POC.RX {
    public static class CustomRetryPocOperatorFixed {
        public static IObservable<T> RetryWithContext<T>(this IObservable<T> source, int retryCount,
            AsyncLocal<string> context, List<string> log) {
            return Observable.Create<T>(observer => {
                var attempts = 0;
                
                var serialDisposable = new SerialDisposable();
                string lastCapturedContext = null;

                void SubscribeToSource() {
                    attempts++;
                    log.Add($"[CustomRetry] Subscribing to source. Attempt {attempts}.");
                    serialDisposable.Disposable = source.Subscribe(observer.OnNext, ex => {
                        lastCapturedContext = context.Value;
                        log.Add($"[CustomRetry] OnError. Captured context is '{lastCapturedContext ?? "null"}'.");

                        if (attempts < retryCount) {
                            log.Add("[CustomRetry] Retrying...");
                            SubscribeToSource();
                        }
                        else {
                            log.Add($"[CustomRetry] Retries exhausted. Restoring context to '{lastCapturedContext ?? "null"}' and forwarding error.");
                            context.Value = lastCapturedContext;
                            observer.OnError(ex);
                        }
                    }, observer.OnCompleted);
                }

                SubscribeToSource();
                // MODIFICATION: Return the SerialDisposable itself.
                return serialDisposable;
            });
        }
    }

    [TestFixture]
    public class CustomRetryOperatorPoc {
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
                    _ => Observable.Throw<Unit>(new InvalidOperationException("Inner Failure"))
                );
            });
        }

        [Test]
        public void CustomRetryOperator_Preserves_Context_And_Cleans_Up_Between_Attempts() {
            var executionLog = new List<string>();
            var subscriptionCount = 0;
            var capturedContext = "CONTEXT_NOT_SET";

            var source = InnerObservableWithContextAndError(() => subscriptionCount++, executionLog);

            var stream = source
                .RetryWithContext(3, TestContext, executionLog)
                .Catch((Exception ex) => {
                    executionLog.Add($"--- OuterCatch Executing. Final context is '{TestContext.Value ?? "null"}'.");
                    capturedContext = TestContext.Value;
                    return Observable.Empty<Unit>();
                });

            stream.Subscribe();

            Console.WriteLine("--- Execution Log ---");
            Console.WriteLine(string.Join(Environment.NewLine, executionLog));
            Console.WriteLine("---------------------");

            // Assert
            capturedContext.ShouldBe("INNER_CONTEXT");
            subscriptionCount.ShouldBe(3);

            // Assert that the context was cleared between attempts
            executionLog.ShouldContain(log => log == "-- Attempt Start. Context before Using: 'null'.");
        }
    }
}