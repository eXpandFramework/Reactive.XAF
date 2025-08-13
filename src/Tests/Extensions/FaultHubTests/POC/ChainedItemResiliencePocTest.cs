using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Tests.FaultHubTests.POC {
    [TestFixture]
    public class ChainedItemResiliencePocTest : FaultHubTestBase {
        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> Level3_InnerMostOperation() 
            => Observable.Timer(TimeSpan.FromMilliseconds(20))
                .SelectMany(_ => Observable.Throw<Unit>(new InvalidOperationException("Failure at Level 3")))
                .ChainFaultContext(["Level3"]);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> Level2_IntermediateOperation() 
            => Level3_InnerMostOperation()
                .ChainFaultContext(["Level2"]);
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> Level1_OuterOperation() 
            => Level2_IntermediateOperation()
                .SwitchOnFault(fullyChainedException => {
                    fullyChainedException.Publish();
                    
                    return Observable.Empty<Unit>();
                }, context: ["Level1"]);

        [Test]
        public async Task DeeplyChained_ItemResilience_With_Async_Succeeds()
        {
            var parentStream = Observable.Range(1, 3)
                .SelectMany(_ => Level1_OuterOperation());
            
            var result = await parentStream.Capture();
            
            result.Error.ShouldBeNull("The parent stream should not have received an error.");
            result.IsCompleted.ShouldBeTrue("The parent stream should have completed.");
            
            BusEvents.Count.ShouldBe(3);
            
            var finalException = BusEvents.First().ShouldBeOfType<FaultHubException>();
            var allContexts = finalException.AllContexts().ToArray();
            
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