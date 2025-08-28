using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;

namespace Xpand.Extensions.Tests.FaultHubTests.POC {
    public static class EagerVsLazyPocExtensions {
        public static IObservable<string> PushStackFrameLazy(this IObservable<string> source, string frame,
            AsyncLocal<string> context) {
            return Observable.Defer(() => {
                var parentContext = context.Value;
                context.Value = string.IsNullOrEmpty(parentContext) ? frame : $"{parentContext} -> {frame}";
                return source;
            });
        }

        public static IObservable<string> PushStackFrameEager(this IObservable<string> source, string frame,
            AsyncLocal<string> context) {
            var parentContext = context.Value;
            context.Value = string.IsNullOrEmpty(parentContext) ? frame : $"{parentContext} -> {frame}";
            return source;
        }
    }

    [TestFixture]
    public class EagerVsLazyContextPocTests {
        private static readonly AsyncLocal<string> TestContext = new();

        [SetUp]
        public void SetUp() {
            TestContext.Value = null;
        }

        [Test]
        public async Task Eager_PushStackFrame_Preserves_Context_In_SelectMany_Chain() {
            string finalContext = null;

            await Observable.Return("Step1").PushStackFrameEager("Frame1", TestContext)
                .SelectMany(val => Observable.Return(val + "/Step2").PushStackFrameEager("Frame2", TestContext))
                .SelectMany(val => {
                    finalContext = TestContext.Value;
                    return Observable.Return(val + "/Final");
                })
                .LastOrDefaultAsync();

            finalContext.ShouldBe("Frame1 -> Frame2");
        }

        [Test]
        public async Task Lazy_PushStackFrame_Loses_Context_In_SelectMany_Chain() {
            string finalContext = null;

            await Observable.Return("Step1").PushStackFrameLazy("Frame1", TestContext)
                .SelectMany(val => Observable.Return(val + "/Step2").PushStackFrameLazy("Frame2", TestContext))
                .SelectMany(val => {
                    finalContext = TestContext.Value;
                    return Observable.Return(val + "/Final");
                })
                .LastOrDefaultAsync();

            finalContext.ShouldBe("Frame1 -> Frame2");
        }
    }
}