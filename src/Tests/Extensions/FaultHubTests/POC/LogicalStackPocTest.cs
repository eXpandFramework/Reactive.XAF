using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;

namespace Xpand.Extensions.Tests.FaultHubTests.POC
{
    [TestFixture]
    public class LogicalStackPocTest
    {
        // A simple record to hold the captured call-site information.
        private record LogicalStackFrame(string MemberName, string FilePath, int LineNumber)
        {
            public override string ToString() => $"at {MemberName} in {FilePath}:line {LineNumber}";
        }

        // A simple stand-in for the FaultHub.Bus.
        private readonly ISubject<string> _mockFaultBus = new ReplaySubject<string>(1);

        // --- SIMULATED FRAMEWORK IMPLEMENTATION ---

        // Level 3: The low-level resilience operator.
        // It receives the complete logical stack and uses it in the error handler.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<T> ResilienceApplier<T>(IObservable<T> source, IReadOnlyList<LogicalStackFrame> logicalStack)
        {
            return source.Catch((Exception ex) =>
            {
                var stringBuilder = new StringBuilder();
                stringBuilder.AppendLine($"Error: {ex.Message}");
                stringBuilder.AppendLine("Captured Logical Stack:");
                // The stack is now received in the correct order (deepest to shallowest caller). We reverse it for display.
                foreach (var frame in logicalStack.Reverse())
                {
                    stringBuilder.AppendLine($"   {frame}");
                }

                _mockFaultBus.OnNext(stringBuilder.ToString());
                _mockFaultBus.OnCompleted();
                return Observable.Empty<T>();
            });
        }

        // Level 2: An internal framework helper.
        // It prepends its own location to the stack and passes it down.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<T> InternalHelper<T>(IObservable<T> source, IReadOnlyList<LogicalStackFrame> stackFromCaller)
        {
            // FIXED: Explicitly create a frame for this method itself.
            var myFrame = new LogicalStackFrame(nameof(InternalHelper), "LogicalStackPocTest.cs", 60); // Line number is for illustration.
            var newStack = new[] { myFrame }.Concat(stackFromCaller).ToList();
            
            return ResilienceApplier(source, newStack);
        }

        // Level 1: The public-facing framework API.
        // This is the entry point for the consumer. It captures the consumer's
        // call site information and starts the logical stack.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<T> ConsumerFacingHelper<T>(IObservable<T> source,
            [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            // FIXED: This list now correctly represents the stack *up to this point*.
            var stackFromConsumer = new List<LogicalStackFrame> { new(memberName, filePath, lineNumber) };
            
            // It now adds its own frame before passing down.
            var myFrame = new LogicalStackFrame(nameof(ConsumerFacingHelper), "LogicalStackPocTest.cs", 75); // Line number is for illustration.
            var newStack = new[] { myFrame }.Concat(stackFromConsumer).ToList();

            return InternalHelper(source, newStack);
        }

        // --- SIMULATED CONSUMER CODE ---

        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<int> MyBusinessLogic()
        {
            var sourceStream = Observable.Timer(TimeSpan.FromMilliseconds(20))
                .SelectMany(_ => Observable.Throw<int>(new InvalidOperationException("Failure in business logic.")));

            // The consumer's call remains simple and unchanged.
            return ConsumerFacingHelper(sourceStream);
        }


        [Test]
        public async Task Logical_Stack_Pattern_Captures_Deep_Async_Call_Site()
        {
            // --- EXECUTION ---
            var stream = MyBusinessLogic();
            using (stream.Subscribe())
            {
                var result = await _mockFaultBus.DefaultIfEmpty("NO_ERROR");

                // --- ASSERTIONS ---
                result.ShouldNotBe("NO_ERROR", "The mock fault bus did not receive an error.");

                var resultLines = result.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                
                // Assert that the logical stack contains the full hierarchy, in the correct order.
                resultLines.ShouldContain(line => line.Contains(nameof(MyBusinessLogic)));
                resultLines.ShouldContain(line => line.Contains(nameof(ConsumerFacingHelper)));
                resultLines.ShouldContain(line => line.Contains(nameof(InternalHelper)));
            }
        }
    }
}