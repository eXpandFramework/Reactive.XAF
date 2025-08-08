using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using NUnit.Framework;
using Shouldly;

namespace Xpand.Extensions.Tests.FaultHubTests;
public class RxExceptionStackTraceTests
{
    private class DeliberateRxException : Exception { }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private long MethodInProjectionThatThrows(long value) => throw new DeliberateRxException();

    [Test]
    public void StackTrace_WhenThrownInObservable_IsFromSchedulerThread_NoAwait()
    {
        // This test uses a compositional, await-free approach to verify that
        // an exception is delivered with a clean stack trace from the scheduler
        // thread, without any enhancement from the calling/subscribing context.

        // Arrange
        Exception caughtException = null;
        var signal = new ManualResetEvent(false);

        var sequence = Observable
            .Return(0L)                               // 1. Produce a value on the current thread.
            .ObserveOn(ThreadPoolScheduler.Instance)  // 2. Shift subsequent operators to a background thread.
            .Select(MethodInProjectionThatThrows);    // 3. Throw an exception on that background thread.

        // Act
        using (sequence.Subscribe(
                   onNext: _ => signal.Set(), // This path should not be taken.
                   onError: ex =>
                   {
                       // This is the expected path. Capture the exception and signal completion.
                       caughtException = ex;
                       signal.Set();
                   },
                   onCompleted: () => signal.Set() // This path should not be taken.
               ))
        {
            // Block the main thread, waiting for a signal from any of the handlers.
            bool wasSignaled = signal.WaitOne(TimeSpan.FromSeconds(5));
            wasSignaled.ShouldBeTrue("The Rx sequence did not signal completion or error within the timeout.");
        }

        // Assert
        // Verify that the onError path was taken by checking the captured exception.
        caughtException.ShouldNotBeNull("The sequence completed without erroring as expected.");
        caughtException.ShouldBeOfType<DeliberateRxException>();

        string stackTrace = caughtException.StackTrace;
        stackTrace.ShouldNotBeNullOrEmpty();
            
        // 1. The original throwing method's frame must be present.
        stackTrace.ShouldContain(nameof(MethodInProjectionThatThrows));
            
        // 2. The calling context (this test method) must be absent.
        stackTrace.ShouldNotContain(nameof(StackTrace_WhenThrownInObservable_IsFromSchedulerThread_NoAwait));
            
        // 3. The async/await enhancement marker must be absent.
        stackTrace.ShouldNotContain("End of stack trace from previous location");
    }
}