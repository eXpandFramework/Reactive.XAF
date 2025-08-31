using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Tests.Reactive.ProofOfConcepts {
    
    [TestFixture]
    public class AsyncLocalFlowInSelectManyPoc {
        private static readonly AsyncLocal<string> TestSnapshotContext = new();
        private string _capturedContextInInnerOperator;

        private IObservable<T> SetContextAndRun<T>(IObservable<T> source, string context) {
            return Observable.Defer(() => {
                TestSnapshotContext.Value = context;
                return source.Finally(() => TestSnapshotContext.Value = null);
            });
        }

        private IObservable<T> CaptureContextOnError<T>(IObservable<T> source) {
            return source.Materialize()
                .Do(notification => {
                    if (notification.Kind == NotificationKind.OnError) {
                        _capturedContextInInnerOperator = TestSnapshotContext.Value;
                    }
                })
                .Dematerialize();
        }

        [Test]
        public async Task Context_Set_Outside_SelectMany_Is_Preserved_To_Operator_On_Inner_Failing_Stream() {
            var source = Observable.Return("one-item");

            var transaction = source.ToList()
                .SelectMany(_ => CaptureContextOnError(Observable.Throw<Unit>(new InvalidOperationException())));

            var transactionWithContext = SetContextAndRun(transaction, "EXPECTED_CONTEXT");

            await transactionWithContext.Capture();

            _capturedContextInInnerOperator.ShouldBe("EXPECTED_CONTEXT");
        }
        
    

        [Test]
        public async Task Context_Set_In_First_SelectMany_Is_Preserved_In_Second_Failing_SelectMany() {
            _capturedContextInInnerOperator = null;

            var secondFailingPart = CaptureContextOnError(Observable.Throw<Unit>(new InvalidOperationException()));

            var chain = Observable.Return(Unit.Default)
                .SelectMany(_ => {
                    TestSnapshotContext.Value = "CONTEXT_FROM_PART_1";
                    return Observable.Return(Unit.Default);
                })
                .SelectMany(_ => secondFailingPart);

            await chain.Capture();

            _capturedContextInInnerOperator.ShouldBe("CONTEXT_FROM_PART_1");
        }
    }
}