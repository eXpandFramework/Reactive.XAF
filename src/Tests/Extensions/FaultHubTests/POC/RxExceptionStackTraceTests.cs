using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using NUnit.Framework;
using Shouldly;

namespace Xpand.Extensions.Tests.FaultHubTests.POC {
    public class RxExceptionStackTraceTests {
        private class DeliberateRxException : Exception { }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private long MethodInProjectionThatThrows(long value) => throw new DeliberateRxException();

        [Test]
        public void StackTrace_WhenThrownInObservable_IsFromSchedulerThread_NoAwait() {
            Exception caughtException = null;
            var signal = new ManualResetEvent(false);

            var sequence = Observable.Return(0L) 
                .ObserveOn(ThreadPoolScheduler.Instance)
                .Select(MethodInProjectionThatThrows); 
            
            using (sequence.Subscribe(_ => signal.Set(), 
                       ex => {
                           caughtException = ex;
                           signal.Set();
                       },
                       () => signal.Set() 
                   )) {

                var wasSignaled = signal.WaitOne(TimeSpan.FromSeconds(5));
                wasSignaled.ShouldBeTrue("The Rx sequence did not signal completion or error within the timeout.");
            }
            
            caughtException.ShouldNotBeNull("The sequence completed without erroring as expected.");
            caughtException.ShouldBeOfType<DeliberateRxException>();

            var stackTrace = caughtException.StackTrace;
            stackTrace.ShouldNotBeNullOrEmpty();
            
            stackTrace.ShouldContain(nameof(MethodInProjectionThatThrows));
            
            stackTrace.ShouldNotContain(nameof(StackTrace_WhenThrownInObservable_IsFromSchedulerThread_NoAwait));
            
            stackTrace.ShouldNotContain("End of stack trace from previous location");
        }
    }
}