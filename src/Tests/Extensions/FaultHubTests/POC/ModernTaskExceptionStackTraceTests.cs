using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;

namespace Xpand.Extensions.Tests.FaultHubTests.POC {
    public class ModernTaskExceptionStackTraceTests {
        private class DeliberateTaskException : Exception { }


        [MethodImpl(MethodImplOptions.NoInlining)]
        private void MethodThatThrows() {
            throw new DeliberateTaskException();
        }

        [Test]
        [Ignore("")]
        public async Task StackTrace_WhenAwaited_IsEnhancedWithCallerContext() {
            var taskThatThrows = Task.Run(MethodThatThrows);
            
            var exception = await Should.ThrowAsync<DeliberateTaskException>(taskThatThrows);
            
            var stackTrace = exception.StackTrace;
            stackTrace.ShouldNotBeNullOrEmpty();
            
            stackTrace.ShouldContain(nameof(MethodThatThrows));
            
            stackTrace.ShouldContain("End of stack trace from previous location");
            
            stackTrace.ShouldContain(nameof(StackTrace_WhenAwaited_IsEnhancedWithCallerContext));
            
            var originalFrameIndex = stackTrace.IndexOf(nameof(MethodThatThrows), StringComparison.Ordinal);
            var awaitingFrameIndex = stackTrace.IndexOf(nameof(StackTrace_WhenAwaited_IsEnhancedWithCallerContext),
                StringComparison.Ordinal);

            originalFrameIndex.ShouldNotBe(-1, "Original frame was not found.");
            awaitingFrameIndex.ShouldNotBe(-1, "Awaiting frame was not found.");
            originalFrameIndex.ShouldBeLessThan(awaitingFrameIndex);
        }
    }
}