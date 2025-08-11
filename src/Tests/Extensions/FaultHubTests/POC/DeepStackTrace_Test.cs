using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;

namespace Xpand.Extensions.Tests.FaultHubTests.POC
{
    [TestFixture]
    public class IsolatedResiliencePocTest
    {
        // A simple stand-in for the real AsyncLocal<StackTrace> in your framework.
        private static readonly AsyncLocal<StackTrace> IsolatedInvocationTrace = new();

        // A simple stand-in for the FaultHub.Bus.
        private readonly ISubject<string> _mockFaultBus = new ReplaySubject<string>(1);

        /// <summary>
        /// This is a stand-in for ChainFaultContext. Its only job is to
        /// capture the stack trace and put it into the AsyncLocal context
        /// when its resulting stream is subscribed to.
        /// </summary>
        private IObservable<T> CaptureContext<T>(IObservable<T> source)
        {
            var trace = new StackTrace(1, true); // Capture stack, skip CaptureContext frame itself.
            return Observable.Using(
                () =>
                {
                    IsolatedInvocationTrace.Value = trace;
                    return System.Reactive.Disposables.Disposable.Create(() => IsolatedInvocationTrace.Value = null);
                },
                _ => source
            );
        }

        /// <summary>
        /// This is a stand-in for an item-resilience operator like SelectItemResilient.
        /// It applies the resilience pattern to a given stream.
        /// </summary>
        private IObservable<T> ApplyIsolatedResilience<T>(IObservable<T> source)
        {
            // The key is that the context is captured *before* resilience is applied.
            return CaptureContext(source).Catch((Exception ex) =>
            {
                // In the Catch block, read the context from AsyncLocal.
                var capturedTrace = IsolatedInvocationTrace.Value;
                var stackTraceString = capturedTrace != null
                    ? string.Join(Environment.NewLine, capturedTrace.GetFrames().Select(f => "   at " + f.GetMethod().DeclaringType.FullName + "." + f.GetMethod().Name))
                    : "   ERROR: No stack trace was found in AsyncLocal context.";

                _mockFaultBus.OnNext($"Error: {ex.Message}{Environment.NewLine}Captured Invocation Stack:{Environment.NewLine}{stackTraceString}");
                _mockFaultBus.OnCompleted();
                return Observable.Empty<T>();
            });
        }

        // Level 2: An intermediate helper method.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<int> PocBusinessLogicHelper(IObservable<int> source)
        {
            return PocFrameworkHelper(source);
        }

        // Level 3: A framework-level helper that uses the resilience pattern.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<int> PocFrameworkHelper(IObservable<int> source)
        {
            // This applies our isolated resilience pattern.
            return ApplyIsolatedResilience(source.Select(val =>
            {
                if (val == 2) throw new InvalidOperationException("Isolated deep failure");
                return val;
            }));
        }

        [Test]
        public async Task Isolated_Pattern_Captures_Full_Call_Hierarchy_On_Async_Error()
        {
            // Level 1: The top-level "user code" call.
            var stream = PocBusinessLogicHelper(Enumerable.Range(1, 3).ToObservable(System.Reactive.Concurrency.TaskPoolScheduler.Default));

            // Subscribe to the stream to activate the pipeline. This is the missing step.
            // We can use a TestObserver or a simple Subscribe, since we only care about the fault bus.
            using (stream.Subscribe())
            {
                // The await on the fault bus remains the same. It will now receive a notification.
                var result = await _mockFaultBus.DefaultIfEmpty("NO_ERROR");
            
                result.ShouldNotBe("NO_ERROR", "The mock fault bus did not receive an error.");
            
                // Assert that the captured stack trace string contains the full hierarchy.
                result.ShouldContain(nameof(Isolated_Pattern_Captures_Full_Call_Hierarchy_On_Async_Error));
                result.ShouldContain(nameof(PocBusinessLogicHelper));
                result.ShouldContain(nameof(PocFrameworkHelper));
            }
        }    }
}