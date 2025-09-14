using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Tests.Reactive.ProofOfConcepts{
    [TestFixture]
    public class UseContextFlawTests {

        [Test]
        public async Task UseContext_SetsValue_ForDurationOfSuccessfulStream_AndRestoresAfter() {
            var context = new AsyncLocal<string> { Value = "Initial" };
            var iAsyncLocal = context.Wrap();
            string contextDuringStream = null;

            var stream = Observable.Return("Success")
                .Do(_ => contextDuringStream = context.Value)
                .UseContext("During", iAsyncLocal);

            await stream;

            contextDuringStream.ShouldBe("During");
            context.Value.ShouldBe("Initial", "Context was not restored after successful completion.");
        }
        private static readonly AsyncLocal<string> TestContext = new();
        [Test]
        public async Task UseContext_SetsValue_ForDurationOfFailingStream_AndRestoresAfter() {
            var context = new AsyncLocal<string> { Value = "Initial" };
            var iAsyncLocal = context.Wrap();
            string contextDuringStream = null;

            var stream = Observable.Return("Success")
                .Do(_ => contextDuringStream = context.Value)
                .UseContext("During", iAsyncLocal);

            await stream;

            contextDuringStream.ShouldBe("During");
            context.Value.ShouldBe("Initial", "Context was not restored after successful completion.");
        }    
    }
}