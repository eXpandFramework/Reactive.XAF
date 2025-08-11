using System;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using akarnokd.reactive_extensions;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;

namespace Xpand.Extensions.Tests.FaultHubTests.POC {
    // A local, simplified version of the core resilience primitive for this POC.

    [TestFixture]
    public class UnifiedResiliencePoc : FaultHubTestBase {
        // This is a high-level, "Item Resilient" operator built according to the new architecture.
        // It is self-contained and resilient by default.
        private IObservable<int> PocSelectItemResilient(IObservable<int> source,
            [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0) {
            
            return source.SelectMany(i => {
                var itemStream = Observable.Defer(() => {
                    if (i == 2) {
                        return Observable.Throw<int>(new InvalidOperationException("Failure on item 2"));
                    }
                    return Observable.Return(i * 10);
                });

                // It uses the core resilience primitive internally.
                // The context of *this* method (PocSelectItemResilient) is captured automatically.
                return itemStream.ApplyPocResilience(new object[] { i }, memberName, filePath, lineNumber);
            });
        }

        [Test]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void High_Level_Operator_Automatically_Captures_Call_Site_Context() {
            var source = Observable.Range(1, 3);

            // The call site is clean. There is no manual call to PushStackFrame or ContinueOnFault.
            // The resilience is built into the PocSelectItemResilient operator itself.
            var testStream = PocSelectItemResilient(source);

            using var testObserver = testStream.Test();

            testObserver.Items.ShouldBe(new[] { 10, 30 });
            testObserver.CompletionCount.ShouldBe(1);
            testObserver.ErrorCount.ShouldBe(0);

            BusObserver.ItemCount.ShouldBe(1);
            var fault = BusObserver.Items.Single().ShouldBeOfType<FaultHubException>();
            var logicalStack = fault.GetLogicalStackTrace().ToList();

            logicalStack.ShouldNotBeEmpty();
            
            // This assertion proves that the logical stack trace correctly starts with the
            // name of the test method that called the high-level operator.
            // The context was captured and propagated automatically.
            logicalStack[0].MemberName.ShouldBe(nameof(PocSelectItemResilient));
            logicalStack[1].MemberName.ShouldBe(nameof(High_Level_Operator_Automatically_Captures_Call_Site_Context));
        }
    }

    internal static class PocResilienceExtensions {
        public static IObservable<T> ApplyPocResilience<T>(
            this IObservable<T> source, 
            object[] context = null, 
            [CallerMemberName] string memberName = "", 
            [CallerFilePath] string filePath = "", 
            [CallerLineNumber] int lineNumber = 0) {
            
            // This is the core of the new architecture. The primitive operator
            // is responsible for establishing the resilience boundary.
            return source
                .PushStackFrame(memberName, filePath, lineNumber)
                .Catch((Exception ex) => {
                    ex.ExceptionToPublish(context.NewFaultContext(memberName, filePath, lineNumber)).Publish();
                    return Observable.Empty<T>();
                });
        }
    }
}