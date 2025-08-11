using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using akarnokd.reactive_extensions;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;
using Xpand.TestsLib.Common;

namespace Xpand.Extensions.Tests.FaultHubTests.POC
{
    [TestFixture]
    public class ChainedItemResiliencePocTest : FaultHubTestBase
    {
        // LEVEL 3: The innermost operation.
        // It fails asynchronously and starts the context chain.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> Level3_InnerMostOperation() 
            => Observable.Timer(TimeSpan.FromMilliseconds(20))
                .SelectMany(_ => Observable.Throw<Unit>(new InvalidOperationException("Failure at Level 3")))
                .ChainFaultContext(["Level3"]);

        // LEVEL 2: An intermediate operation.
        // It calls the inner operation and adds its own context to the chain.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> Level2_IntermediateOperation() 
            => Level3_InnerMostOperation()
                .ChainFaultContext(["Level2"]);

        // LEVEL 1: The outermost operation within the item's scope.
        // It calls the intermediate operation and uses SwitchOnFault to suppress the final,
        // Fully-chained error, thus achieving item resilience.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> Level1_OuterOperation() 
            => Level2_IntermediateOperation()
                .SwitchOnFault(fullyChainedException => {
                    // This is the key:
                    // 1. We receive the complete, chained exception. The trace is already built.
                    fullyChainedException.Publish();

                    // 2. We return an empty stream to suppress the error, protecting the parent.
                    return Observable.Empty<Unit>();
                }, context: ["Level1"]);

        [Test]
        public async Task DeeplyChained_ItemResilience_With_Async_Succeeds()
        {
            // A parent stream that we need to protect from termination.
            var parentStream = Observable.Range(1, 3)
                .SelectMany(_ => Level1_OuterOperation());

            // Subscribe and wait for the parent stream to complete.
            var testObserver = parentStream.Test();
            await testObserver.AwaitDoneAsync(TimeSpan.FromSeconds(5));

            // --- ASSERTIONS ---

            // 1. PROOF OF SUPPRESSION:
            // The parent stream should not have received any errors and should have completed.
            testObserver.ErrorCount.ShouldBe(0, "The parent stream should not have received an error.");
            testObserver.CompletionCount.ShouldBe(1, "The parent stream should have completed.");

            // 2. PROOF OF TRACING:
            // The FaultHub Bus should have received an exception for each of the 3 items.
            BusObserver.ItemCount.ShouldBe(3);

            // Inspect the first exception to ensure the context is fully chained.
            var finalException = BusObserver.Items.First().ShouldBeOfType<FaultHubException>();
            var allContexts = finalException.AllContexts().ToArray();

            // The contexts should be stacked from outermost (Level1) to innermost (Level3).
            allContexts.ShouldContain("Level1");
            allContexts.ShouldContain("Level2");
            allContexts.ShouldContain("Level3");

            var indexOfLevel1 = Array.IndexOf(allContexts, "Level1");
            var indexOfLevel2 = Array.IndexOf(allContexts, "Level2");
            var indexOfLevel3 = Array.IndexOf(allContexts, "Level3");

            indexOfLevel1.ShouldBeLessThan(indexOfLevel2);
            indexOfLevel2.ShouldBeLessThan(indexOfLevel3);
        }
    }
}