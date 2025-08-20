using System;
using System.Reactive.Linq;
using System.Threading;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.Utility;
using Unit = System.Reactive.Unit;

namespace Xpand.Extensions.Tests.FaultHubTests.POC.RX {
    [TestFixture]
    public class AsyncLocalSynchronousSelectManyPoc {
        private static readonly AsyncLocal<string> PocContext = new();
        private string _capturedInnerContext;

        [SetUp]
        public void SetUp() {
            PocContext.Value = null;
            _capturedInnerContext = "NOT_SET";
        }

        [Test]
        public void Context_Is_Preserved_Inside_Synchronous_Failing_SelectMany() {
            var streamWithOuterContext = Observable.Defer(() => {
                PocContext.Value = "EXPECTED_CONTEXT";

                var transactionStream = Observable.Return("Part 1 completes")
                    .SelectMany(_ => Observable.Throw<Unit>(new InvalidOperationException("Inner Failure"))
                        .DoOnError(_ => {
                                _capturedInnerContext = PocContext.Value;
                                
                            })
                    );

                return transactionStream.Finally(() => PocContext.Value = null);
            });

            var finalStream = streamWithOuterContext
                .Catch((Exception _) => Observable.Empty<Unit>());

            finalStream.Subscribe();

            _capturedInnerContext.ShouldBe("EXPECTED_CONTEXT");
        }
    }
}