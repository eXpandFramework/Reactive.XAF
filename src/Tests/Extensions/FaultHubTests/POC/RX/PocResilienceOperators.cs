using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using akarnokd.reactive_extensions;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Numeric;
using Xpand.TestsLib.Common;
using Unit = System.Reactive.Unit;

namespace Xpand.Extensions.Tests.FaultHubTests.POC.RX {
    

    [SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Global")]
    public record LogicalPocStackFrame(string MemberName, object[] Context = null);

    public class PocFaultHubException(string message, Exception innerException, IReadOnlyList<LogicalPocStackFrame> stack)
        : Exception(message, innerException) {
        public IReadOnlyList<LogicalPocStackFrame> LogicalStackTrace { get; } = stack;
    }

    public static class PocResilienceOperators {
        public static IObservable<T> PushStackFrame<T>(this IObservable<T> source, string frameName) {
            return source.Catch((Exception ex) => {
                var newFrame = new LogicalPocStackFrame(frameName);

                if (ex is not PocFaultHubException fault) {
                    var newStack = new[] { newFrame };
                    return Observable.Throw<T>(new PocFaultHubException("Error in resilient stream", ex, newStack));
                }
                
                var newChainedStack = new[] { newFrame }.Concat(fault.LogicalStackTrace).ToList();
                var newFault = new PocFaultHubException(fault.Message, fault.InnerException, newChainedStack);
                return Observable.Throw<T>(newFault);
            });
        }
    }

    [TestFixture]
    public class CatchAndRethrowPocTests {
        
        private IObservable<Unit> Level3_Work_On_Another_Thread()
            => Observable.Timer(TimeSpan.FromMilliseconds(20))
                .SelectMany(_ => Observable.Throw<Unit>(new InvalidOperationException("Database disconnected")))
                .PushStackFrame("Level3_Work_On_Another_Thread");

        
        private IObservable<Unit> Level2_BusinessLogic()
            => Level3_Work_On_Another_Thread()
                .PushStackFrame("Level2_BusinessLogic");

        
        private IObservable<Unit> Level1_TopLevelOperation()
            => Level2_BusinessLogic()
                .PushStackFrame("Level1_TopLevelOperation");

        [Test]
        public async Task CatchAndRethrow_Builds_Complete_StackTrace_Across_Async_Scheduler_Boundaries() {
            PocFaultHubException capturedFault = null;
            var stream = Level1_TopLevelOperation()
                .Catch((PocFaultHubException ex) => {
                    capturedFault = ex;
                    return Observable.Empty<Unit>();
                });
            
            await stream.Test().AwaitDoneAsync(1.Seconds());

    
            capturedFault.ShouldNotBeNull();
            capturedFault.InnerException.ShouldBeOfType<InvalidOperationException>()
                .Message.ShouldBe("Database disconnected");

            var logicalStack = capturedFault.LogicalStackTrace;
            logicalStack.Count.ShouldBe(3, "The logical stack should contain a frame from each level.");

    
            logicalStack[0].MemberName.ShouldBe("Level1_TopLevelOperation");
            logicalStack[1].MemberName.ShouldBe("Level2_BusinessLogic");
            logicalStack[2].MemberName.ShouldBe("Level3_Work_On_Another_Thread");
        }
    }
}