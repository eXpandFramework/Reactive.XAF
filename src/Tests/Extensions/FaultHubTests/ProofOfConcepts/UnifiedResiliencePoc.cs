using System;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.Relay;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Tests.FaultHubTests.ProofOfConcepts {
    [TestFixture]
    public class UnifiedResiliencePoc : FaultHubTestBase {
        
        
        private IObservable<int> PocSelectItemResilient(IObservable<int> source) {
            return source.SelectMany(i => {
                var itemStream = Observable.Defer(() => {
                    if (i == 2) return Observable.Throw<int>(new InvalidOperationException("Failure on item 2"));
                    return Observable.Return(i * 10);
                });

                
                return itemStream.ContinueOnFault(context:[i]);
            });
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

            
            
            logicalStack.Count.ShouldBe(1);
            logicalStack[0].MemberName.ShouldBe(nameof(PocSelectItemResilient));
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
                    ex.ExceptionToPublish(FaultHub.LogicalStackContext.Value.NewFaultContext(context,null, memberName,
                        filePath, lineNumber)).Publish();
                    return Observable.Empty<T>();
                });
        }
    }
}