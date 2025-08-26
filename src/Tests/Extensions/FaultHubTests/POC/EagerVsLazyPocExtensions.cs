using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;

namespace Xpand.Extensions.Tests.FaultHubTests.POC
{
    // Helper operators to model the two different PushStackFrame implementations
    public static class EagerVsLazyPocExtensions
    {
        // LAZY (FAILING) PATTERN: Logic is wrapped in Defer.
        // The context is not modified until subscription.
        public static IObservable<string> PushStackFrameLazy(this IObservable<string> source, string frame, AsyncLocal<string> context)
        {
            return Observable.Defer(() =>
            {
                var parentContext = context.Value;
                context.Value = string.IsNullOrEmpty(parentContext) ? frame : $"{parentContext} -> {frame}";
                return source;
            });
        }

        // EAGER (WORKING) PATTERN: Logic is NOT wrapped in Defer.
        // The context is modified immediately when the method is called.
        public static IObservable<string> PushStackFrameEager(this IObservable<string> source, string frame, AsyncLocal<string> context)
        {
            var parentContext = context.Value;
            context.Value = string.IsNullOrEmpty(parentContext) ? frame : $"{parentContext} -> {frame}";
            return source;
        }
    }

    [TestFixture]
    public class EagerVsLazyContextPocTests
    {
        private static readonly AsyncLocal<string> TestContext = new();

        [SetUp]
        public void SetUp()
        {
            TestContext.Value = null;
        }

        [Test]
        public async Task Eager_PushStackFrame_Preserves_Context_In_SelectMany_Chain()
        {
            string finalContext = null;

            // Using the EAGER implementation
            await Observable.Return("Step1").PushStackFrameEager("Frame1", TestContext)
                .SelectMany(val => Observable.Return(val + "/Step2").PushStackFrameEager("Frame2", TestContext))
                .SelectMany(val =>
                {
                    // This probe will run after Frame1 and Frame2 have been eagerly pushed.
                    finalContext = TestContext.Value;
                    return Observable.Return(val + "/Final");
                })
                .LastOrDefaultAsync();

            // The full stack is preserved.
            finalContext.ShouldBe("Frame1 -> Frame2");
        }
        
        [Test]
        public async Task Lazy_PushStackFrame_Loses_Context_In_SelectMany_Chain()
        {
            string finalContext = null;

            // Using the LAZY (Defer) implementation
            await Observable.Return("Step1").PushStackFrameLazy("Frame1", TestContext)
                .SelectMany(val => Observable.Return(val + "/Step2").PushStackFrameLazy("Frame2", TestContext))
                .SelectMany(val =>
                {
                    // The context from "Frame1" is lost by the first SelectMany
                    // before the Defer for "Frame2" is executed.
                    finalContext = TestContext.Value;
                    return Observable.Return(val + "/Final");
                })
                .LastOrDefaultAsync();

            // The parent stack ("Frame1") is lost.
            finalContext.ShouldBe("Frame2");
        }
    }
}