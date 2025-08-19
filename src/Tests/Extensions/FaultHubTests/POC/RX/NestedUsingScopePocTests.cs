using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using NUnit.Framework;
using Shouldly;
using Unit = System.Reactive.Unit;

namespace Xpand.Extensions.Tests.FaultHubTests.POC.RX {
    [TestFixture]
    public class NestedUsingScopePocTests {
        private static readonly AsyncLocal<string> TestContext = new();
        
        private static IObservable<Unit> InnerObservableWithError() {
            return Observable.Using(
                () => {
                    Console.WriteLine("[PoC] Inner scope entered. Setting context to 'INNER_CONTEXT'.");
                    TestContext.Value = "INNER_CONTEXT";
                    return Disposable.Create(() => {
                        Console.WriteLine("[PoC] Inner scope's Dispose action is running. Setting context to null.");
                        TestContext.Value = null;
                    });
                },
                _ => {
                    Console.WriteLine("[PoC] Inner observable is about to throw an exception.");
                    return Observable.Throw<Unit>(new InvalidOperationException("Inner Failure"));
                }
            );
        }

        [Test]
        public void Catch_On_Outer_Scope_Preserves_Context_From_Failing_Inner_Scope() {
            var capturedContext = "CONTEXT_NOT_SET";

            var stream = InnerObservableWithError()
                .Catch((Exception _) => {
                    Console.WriteLine("[PoC] Outer Catch block is executing. Capturing context.");
                    capturedContext = TestContext.Value;
                    return Observable.Empty<Unit>();
                });
            
            stream.Subscribe();

            
            capturedContext.ShouldBe("INNER_CONTEXT");
            
        }
        
        [Test]
        public void SelectMany_Preserves_Context_From_Failing_Inner_Scope() {
            string capturedContext = "CONTEXT_NOT_SET";

            var stream = Observable.Return(Unit.Default)
                .SelectMany(_ => InnerObservableWithError())
                .Catch((Exception _) => {
                    Console.WriteLine("[PoC-SelectMany] Outer Catch block is executing. Capturing context.");
                    capturedContext = TestContext.Value;
                    return Observable.Empty<Unit>();
                });
            
            stream.Subscribe();
            
            capturedContext.ShouldBe("INNER_CONTEXT");
            
        }
    }
}