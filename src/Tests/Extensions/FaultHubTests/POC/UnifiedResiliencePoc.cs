using System;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Tests.FaultHubTests.POC {

    [TestFixture]
    public class UnifiedResiliencePoc : FaultHubTestBase {
// MODIFICATION: The entire method is restructured to correctly compose the resilience operators.
        private IObservable<int> PocSelectItemResilient(IObservable<int> source,
            [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0) {
    
            // MODIFICATION: The SelectMany now builds the inner part of the logical stack.
            return source.SelectMany(i => {
                    var itemStream = Observable.Defer(() => {
                        if (i == 2) {
                            return Observable.Throw<int>(new InvalidOperationException("Failure on item 2"));
                        }
                        return Observable.Return(i * 10);
                    });
        
                    // This is the correct pattern for building a nested logical stack and applying suppression.
                    // 1. The stream for the individual item is created.
                    // 2. We push the frame for this helper method itself.
                    // 3. We then use ContinueOnFault, which pushes the CALLER's frame (the test method)
                    //    and provides the error suppression.
                    return itemStream
                        .PushStackFrame()
                        .ContinueOnFault([i], memberName, filePath, lineNumber);
                })
                // MODIFICATION: ContinueOnFault is now the final operator. It acts as the suppression boundary
                // and adds its own frame ("PocSelectItemResilient") to the stack before handling the error.
                .ContinueOnFault();
        }
        [Test]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public async Task High_Level_Operator_Automatically_Captures_Call_Site_Context() {
            var source = Observable.Range(1, 3);
            var testStream = PocSelectItemResilient(source);

            var result = await testStream.Capture();

            result.Items.ShouldBe([10, 30]);
            result.IsCompleted.ShouldBe(true);
            result.Error.ShouldBeNull();

            BusEvents.Count.ShouldBe(1);
            var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            var logicalStack = fault.LogicalStackTrace.ToList();

            logicalStack.ShouldNotBeEmpty();
            
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
            
            return source
                .PushStackFrame(memberName, filePath, lineNumber)
                .Catch((Exception ex) => {
                    ex.ExceptionToPublish(context.NewFaultContext(FaultHub.LogicalStackContext.Value,memberName, filePath, lineNumber)).Publish();
                    return Observable.Empty<T>();
                });
        }
    }
}