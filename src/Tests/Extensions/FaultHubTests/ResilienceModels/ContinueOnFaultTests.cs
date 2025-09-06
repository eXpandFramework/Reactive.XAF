using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Tests.FaultHubTests.ResilienceModels {
    [TestFixture]
    public class ContinueOnFaultTests  : FaultHubTestBase {
        [Test]
        public async Task Suppresses_Error_Publishes_To_Bus_And_Captures_Caller_Context_Synchronously() {
            var source = Observable.Throw<int>(new InvalidOperationException("Sync Failure"));

            var result = await source.ContinueOnFault(context: ["MyContext"]).Capture();

            result.Error.ShouldBeNull();
            result.IsCompleted.ShouldBeTrue();

            BusEvents.Count.ShouldBe(1);
            var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            fault.InnerException.ShouldBeOfType<InvalidOperationException>();
            fault.LogicalStackTrace.First().MemberName.ShouldBe(nameof(Suppresses_Error_Publishes_To_Bus_And_Captures_Caller_Context_Synchronously));
            fault.AllContexts.ShouldContain("MyContext");
        }

        [Test]
        public async Task Suppresses_Error_And_Publishes_To_Bus_Asynchronously() {
            var source = Observable.Timer(TimeSpan.FromMilliseconds(20))
                .SelectMany(_ => Observable.Throw<int>(new InvalidOperationException("Async Failure")));

            var result = await source.ContinueOnFault().Capture();
            
            result.Error.ShouldBeNull();
            result.IsCompleted.ShouldBeTrue();
            BusEvents.Count.ShouldBe(1);
            BusEvents.Single().InnerException.ShouldBeOfType<InvalidOperationException>();
        }

        [Test]
        public async Task Works_With_Retry_Strategy_And_Publishes_Final_Error() {
            var attemptCount = 0;
            var source = Observable.Defer(() => {
                attemptCount++;
                return Observable.Throw<Unit>(new InvalidOperationException("Transient Error"));
            });

            await source.ContinueOnFault(s => s.Retry(3)).Capture();
            
            attemptCount.ShouldBe(3);
            BusEvents.Count.ShouldBe(1);
            BusEvents.Single().InnerException.ShouldBeOfType<InvalidOperationException>();
        }

        [Test]
        public async Task Handles_Exception_From_Upstream_Disposal() {
            var resource = new TestResource { OnDispose = () => throw new InvalidOperationException("Dispose failed.") };
            var sourceWithFailingDispose = Observable.Using(() => resource, _ => Observable.Return(42));
            
            var result = await sourceWithFailingDispose.ContinueOnFault().Capture();
            
            result.Items.ShouldBe([42]);
            result.IsCompleted.ShouldBeTrue();
            
            BusEvents.Count.ShouldBe(1);
            BusEvents.Single().InnerException.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Dispose failed.");
        }
        
        [Test]
        public async Task ContinueOnFault_With_DefaultIfEmpty_Provides_Fallback_Value() {
            var source = Observable.Throw<string>(new InvalidOperationException("Failure"));

            var result = await source
                .ContinueOnFault(context: ["MyFallbackContext"])
                .DefaultIfEmpty("DefaultValue")
                .Capture();

            result.Error.ShouldBeNull();
            result.IsCompleted.ShouldBeTrue();
            result.Items.ShouldHaveSingleItem();
            result.Items.Single().ShouldBe("DefaultValue");

            BusEvents.Count.ShouldBe(1);
            BusEvents.Single().ShouldBeOfType<FaultHubException>()
                .Context.UserContext.ShouldContain("MyFallbackContext");
        }
        
        [Test]
        public async Task After_SelectMany_Terminates_On_First_Inner_Failure_And_Suppresses_Outer_Stream() {
            var source = Observable.Range(1, 3);
            var processedItems = new List<int>();

            var result = await source.SelectMany(item => {
                    processedItems.Add(item);
                    if (item == 2) {
                        return Observable.Throw<int>(new InvalidOperationException("Failure on item 2"));
                    }
                    return Observable.Return(item * 10);
                })
                .ContinueOnFault(context: ["OuterBoundary"])
                .Capture();

            processedItems.Count.ShouldBe(2, "Processing should have stopped after the second item failed.");
            processedItems.ShouldBe([1, 2]);

            result.Items.ShouldHaveSingleItem("Only the result from the first item should have been emitted.");
            result.Items.Single().ShouldBe(10);
    
            result.Error.ShouldBeNull("ContinueOnFault should have suppressed the error and completed the stream.");
            result.IsCompleted.ShouldBeTrue();

            BusEvents.Count.ShouldBe(1, "The single failure should have been published.");
            var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            fault.InnerException.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Failure on item 2");
            fault.Context.UserContext.ShouldContain("OuterBoundary");
        }
    }
}