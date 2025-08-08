using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;

namespace Xpand.Extensions.Tests.FaultHubTests
{
    public class ModernTaskExceptionStackTraceTests
    {
        private class DeliberateTaskException : Exception { }

        
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void MethodThatThrows()
        {
            throw new DeliberateTaskException();
        }

        [Test][Ignore("")]
        public async Task StackTrace_WhenAwaited_IsEnhancedWithCallerContext()
        {
            // This test demonstrates modern .NET's behavior for async exceptions.
            // The runtime enhances stack traces across await boundaries to provide a
            // complete logical call flow for easier debugging.

            // Arrange
            var taskThatThrows = Task.Run(MethodThatThrows);

            // Act
            var exception = await Should.ThrowAsync<DeliberateTaskException>(taskThatThrows);

            // Assert
            string stackTrace = exception.StackTrace;
            stackTrace.ShouldNotBeNullOrEmpty();

            // 1. Verify the original stack frame from the worker thread is present.
            stackTrace.ShouldContain(nameof(MethodThatThrows));

            // 2. Verify the special marker linking the two parts of the stack is present.
            stackTrace.ShouldContain("End of stack trace from previous location");

            // 3. Verify the awaiting context (this test method) is appended to the stack trace.
            stackTrace.ShouldContain(nameof(StackTrace_WhenAwaited_IsEnhancedWithCallerContext));

            // 4. Verify the frame order is correct: the original frame must appear
            // before the awaiting frame in the stack trace string.
            int originalFrameIndex = stackTrace.IndexOf(nameof(MethodThatThrows), StringComparison.Ordinal);
            int awaitingFrameIndex = stackTrace.IndexOf(nameof(StackTrace_WhenAwaited_IsEnhancedWithCallerContext), StringComparison.Ordinal);

            originalFrameIndex.ShouldNotBe(-1, "Original frame was not found.");
            awaitingFrameIndex.ShouldNotBe(-1, "Awaiting frame was not found.");
            originalFrameIndex.ShouldBeLessThan(awaitingFrameIndex);
        }
    }
}