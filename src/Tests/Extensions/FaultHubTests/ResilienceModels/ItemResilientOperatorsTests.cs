using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.Relay;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Tests.FaultHubTests.ResilienceModels {
    [TestFixture]
    public class ItemResilientOperatorsTests  : FaultHubTestBase {

        [Test]
        public async Task DoItemResilient_Executes_Action_And_Passes_Item_Through_On_Success() {
            var actionWasCalled = false;
            var source = Observable.Return(42);

            var result = await source.DoItemResilient(item => {
                item.ShouldBe(42);
                actionWasCalled = true;
            }).Capture();

            result.Items.ShouldBe([42]);
            result.IsCompleted.ShouldBeTrue();
            actionWasCalled.ShouldBeTrue();
            BusEvents.Count.ShouldBe(0);
        }
        
        [Test]
        public async Task DeferItemResilient_Suppresses_Error_From_Disposal() {
            var resource = new TestResource { OnDispose = () => throw new InvalidOperationException("Dispose Failed") };
            var source = this.DeferItemResilient(() => Observable.Using(() => resource, _ => Observable.Return(42)));

            var result = await source.Capture();
            
            result.Items.ShouldBe([42]);
            result.IsCompleted.ShouldBeTrue();
            BusEvents.Count.ShouldBe(1);
            BusEvents.Single().InnerException.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Dispose Failed");
        }

        [Test]
        public async Task SelectItemResilient_Suppresses_Error_From_Disposal() {
            var resource = new TestResource { OnDispose = () => throw new InvalidOperationException("Dispose Failed") };
            var source = Observable.Using(() => resource, _ => Observable.Return(1));

            var result = await source.SelectItemResilient(i => i * 10).Capture();

            result.Items.ShouldBe([10]);
            result.IsCompleted.ShouldBeTrue();
            BusEvents.Count.ShouldBe(1);
            BusEvents.Single().InnerException.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Dispose Failed");
        }

        [Test]
        public async Task SelectManyItemResilient_Suppresses_Error_From_Disposal() {
            var resource = new TestResource { OnDispose = () => throw new InvalidOperationException("Dispose Failed") };
            var source = Observable.Using(() => resource, _ => Observable.Return(1));

            var result = await source.SelectManyItemResilient(i => Observable.Return(i * 10)).Capture();

            result.Items.ShouldBe([10]);
            result.IsCompleted.ShouldBeTrue();
            BusEvents.Count.ShouldBe(1);
            BusEvents.Single().InnerException.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Dispose Failed");
        }

        [Test]
        public async Task DoItemResilient_Suppresses_Error_And_Publishes_Fault() {
            var source = Observable.Return(42);

            var result = await source.DoItemResilient(_ => throw new InvalidOperationException("Do Failure")).Capture();
            
            result.Items.ShouldBe([42]);
            result.IsCompleted.ShouldBeTrue();
            result.Error.ShouldBeNull();

            BusEvents.Count.ShouldBe(1);
            var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            fault.InnerException.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Do Failure");
            fault.AllContexts.ShouldContain(42);
        }
        
        [Test]
        public async Task DoItemResilient_Suppresses_Error_From_Disposal() {
            var resource = new TestResource { OnDispose = () => throw new InvalidOperationException("Dispose Failed") };
            var source = Observable.Using(() => resource, _ => Observable.Return(42));
            
            var result = await source.DoItemResilient(i => i.ShouldBe(42)).Capture();
            
            result.Items.ShouldBe([42]);
            result.IsCompleted.ShouldBeTrue();
            BusEvents.Count.ShouldBe(1);
            BusEvents.Single().InnerException.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Dispose Failed");
        }
        
        [Test]
        public async Task SelectItemResilient_With_Retry_Exhausts_Retries_And_Publishes_Final_Error() {
            var attemptCount = 0;
            var source = Observable.Return(10);
            
            var result = await source.SelectItemResilient(_ => {
                attemptCount++;
                throw new InvalidOperationException();
#pragma warning disable CS0162 
                return Observable.Throw<int>(new InvalidOperationException("Selector Failed")).ToList();
#pragma warning restore CS0162 
            }, s => s.Retry(3)).Capture();
            
            result.IsCompleted.ShouldBe(true);
            result.Items.Count.ShouldBe(0);
            
            attemptCount.ShouldBe(3);
            BusEvents.Count.ShouldBe(1);
            var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            fault.AllContexts.ShouldContain(10);
        }

        [Test]
        public async Task DeferItemResilient_Executes_Factory_On_Success() {
            var factoryWasCalled = false;
            var stream = this.DeferItemResilient(() => {
                factoryWasCalled = true;
                return Observable.Return(100);
            });

            var result = await stream.Capture();
            
            result.Items.ShouldBe([100]);
            result.IsCompleted.ShouldBeTrue();
            factoryWasCalled.ShouldBeTrue();
            BusEvents.Count.ShouldBe(0);
        }
        
        [Test]
        public async Task DeferItemResilient_Suppresses_Error_When_Factory_Itself_Throws() {
            var stream = this.DeferItemResilient<int>(() => throw new InvalidOperationException("Factory Failure"));

            var result = await stream.Capture();
            
            result.IsCompleted.ShouldBeTrue();
            result.Error.ShouldBeNull();

            BusEvents.Count.ShouldBe(1);
            BusEvents.Single().InnerException.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Factory Failure");
        }
        
        [Test]
        public async Task DeferItemResilient_Suppresses_Error_When_Produced_Observable_Throws() {
            var stream = this.DeferItemResilient(() => Observable.Throw<int>(new InvalidOperationException("Observable Failure")));

            var result = await stream.Capture();
            
            result.IsCompleted.ShouldBeTrue();
            result.Error.ShouldBeNull();

            BusEvents.Count.ShouldBe(1);
            BusEvents.Single().InnerException.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Observable Failure");
        }
        
        [Test]
        public async Task UsingItemResilient_Succeeds_And_Disposes_Resource() {
            var resource = new TestResource();
            var stream = this.UsingItemResilient(() => resource, _ => Observable.Return(42));
            
            var result = await stream.Capture();
            
            result.Items.ShouldBe([42]);
            result.IsCompleted.ShouldBeTrue();
            resource.IsDisposed.ShouldBeTrue();
            BusEvents.Count.ShouldBe(0);
        }

        [Test]
        public async Task UsingItemResilient_Suppresses_Error_From_Resource_Factory() {
            var stream = this.UsingItemResilient<int, TestResource>(() => throw new InvalidOperationException("Resource Factory Failed"),
                _ => Observable.Empty<int>());
            
            var result = await stream.Capture();
            
            result.IsCompleted.ShouldBeTrue();
            BusEvents.Count.ShouldBe(1);
            BusEvents.Single().InnerException.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Resource Factory Failed");
        }
        
        [Test]
        public async Task UsingItemResilient_Suppresses_Error_From_Observable_Factory_And_Disposes() {
            var resource = new TestResource();
            var stream = this.UsingItemResilient(() => resource,
                _ => Observable.Throw<Unit>(new InvalidOperationException("Observable Factory Failed")));
            
            var result = await stream.Capture();

            result.IsCompleted.ShouldBeTrue();
            resource.IsDisposed.ShouldBeTrue();
            BusEvents.Count.ShouldBe(1);
            BusEvents.Single().InnerException.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Observable Factory Failed");
        }
        
        [Test]
        public async Task UsingItemResilient_Suppresses_Error_From_Disposal() {
            var resource = new TestResource { OnDispose = () => throw new InvalidOperationException("Dispose Failed") };
            var stream = this.UsingItemResilient(() => resource, _ => Observable.Return(42));
            
            var result = await stream.Capture();
            
            result.Items.ShouldBe([42]);
            result.IsCompleted.ShouldBeTrue();
            BusEvents.Count.ShouldBe(1);
            BusEvents.Single().InnerException.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Dispose Failed");
        }
        
        [Test]
        public async Task SelectManyItemResilient_Processes_All_Items_Despite_Inner_Failure() {
            var source = Observable.Range(1, 3);

            var result = await source.SelectManyItemResilient(i => {
                if (i == 2) {
                    return Observable.Throw<int>(new InvalidOperationException("Failure on item 2"));
                }
                return Observable.Return(i * 10);
            }).Capture();

            result.Items.ShouldBe([10, 30]);
            result.IsCompleted.ShouldBeTrue();
            
            BusEvents.Count.ShouldBe(1);
            var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            fault.InnerException.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Failure on item 2");
            fault.AllContexts.ShouldContain(2);
        }

        [Test]
        public async Task Chained_ItemResilience_Operator_Adds_Its_Frame_To_The_Stack() {
            var source = Observable.Throw<Unit>(new InvalidOperationException("Original failure"));

            var chainedStream = source.ChainFaultContext(["UpstreamContext"]);
            var resilientStream = chainedStream.ContinueOnFault();

            await resilientStream.Capture();

            BusEvents.Count.ShouldBe(1);
            var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();

            fault.LogicalStackTrace.ShouldNotBeEmpty("The invocation stack should not be empty.");
            fault.LogicalStackTrace.First().MemberName.ShouldBe(nameof(Chained_ItemResilience_Operator_Adds_Its_Frame_To_The_Stack));
        }
        [Test]
        public async Task ItemResilience_Operator_Preserves_Upstream_FaultHubException_Context() {
            var source = Observable.Throw<Unit>(new InvalidOperationException("Original failure"));

            var chainedStream = source.ChainFaultContext(["UpstreamContext"]);
            var resilientStream = chainedStream.ContinueOnFault(context: ["DownstreamContext"]);

            await resilientStream.Capture();

            BusEvents.Count.ShouldBe(1);
            var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();

            fault.AllContexts.ShouldContain("UpstreamContext", 
                "The upstream context from ChainFaultContext was lost.");

            fault.AllContexts.ShouldNotContain("DownstreamContext",
                "The downstream operator incorrectly overwrote the existing rich context.");
        }
        
        [Test]
        public async Task ItemResilience_Correctly_Wraps_Existing_FaultHubException_Without_Reprocessing_Stack() {
            var upstreamStack = new[] { new LogicalStackFrame("UpstreamWork", "", 0) };
            var upstreamContext = new AmbientFaultContext { BoundaryName = "UpstreamBoundary", LogicalStackTrace = upstreamStack };
            var upstreamFault = new FaultHubException("Upstream Failure", new InvalidOperationException("Root"), upstreamContext);
            var source = Observable.Throw<Unit>(upstreamFault);

            var resilientStream = source.ContinueOnFault(context: ["DownstreamBoundary"]);
            await resilientStream.PublishFaults().Capture();

            BusEvents.Count.ShouldBe(1);
            var finalFault = BusEvents.Single().ShouldBeOfType<FaultHubException>();

            finalFault.Context.InnerContext.ShouldBe(upstreamContext, "The original context from the upstream fault should have been preserved as the InnerContext.");

            finalFault.Context.LogicalStackTrace.Count.ShouldBe(1, 
                "The new context's stack should only contain its own frame, not the entire re-processed upstream stack.");
            finalFault.Context.LogicalStackTrace.Single().MemberName
                .ShouldBe(nameof(ItemResilience_Correctly_Wraps_Existing_FaultHubException_Without_Reprocessing_Stack));
        }
        [Test]
        public async Task SelectManyItemResilient_Preserves_Upstream_FaultHubException_Context() {
            var source = Observable.Return(Unit.Default);
            var failingInnerStream = Observable.Throw<Unit>(new InvalidOperationException("Original failure"))
                                             .ChainFaultContext(["UpstreamContext"]);

            var resilientStream = source.SelectManyItemResilient(_ => failingInnerStream, context: ["DownstreamContext"]);

            await resilientStream.Capture();

            BusEvents.Count.ShouldBe(1);
            var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();

            fault.AllContexts.ShouldContain("UpstreamContext", "The upstream context from ChainFaultContext was lost.");
            fault.AllContexts.ShouldNotContain("DownstreamContext", "The downstream operator incorrectly used the new context for the wrapper.");
        }

        [Test]
        public async Task DoItemResilient_Preserves_Upstream_FaultHubException_Context() {
            var source = Observable.Return(Unit.Default);
            var failingInnerStream = Observable.Throw<Unit>(new InvalidOperationException("Original failure"))
                                             .ChainFaultContext(["UpstreamContext"]);
            
            var upstreamFault = await failingInnerStream.Materialize().Select(n => n.Exception).FirstAsync();

            var resilientStream = source.DoItemResilient(_ => throw upstreamFault, context: ["DownstreamContext"]);

            await resilientStream.Capture();

            BusEvents.Count.ShouldBe(1);
            var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();

            fault.AllContexts.ShouldContain("UpstreamContext");
            fault.AllContexts.ShouldNotContain("DownstreamContext");
        }

        [Test]
        public async Task SelectItemResilient_Preserves_Upstream_FaultHubException_Context() {
            var source = Observable.Return(Unit.Default);
            var failingInnerStream = Observable.Throw<Unit>(new InvalidOperationException("Original failure"))
                                             .ChainFaultContext(["UpstreamContext"]);

            var upstreamFault = await failingInnerStream.Materialize().Select(n => n.Exception).FirstAsync();

            var resilientStream = source.SelectItemResilient<Unit, Unit>(_ => throw upstreamFault, context: ["DownstreamContext"]);

            await resilientStream.Capture();

            BusEvents.Count.ShouldBe(1);
            var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();

            fault.AllContexts.ShouldContain("UpstreamContext");
            fault.AllContexts.ShouldNotContain("DownstreamContext");
        }

        [Test]
        public async Task DeferItemResilient_Preserves_Upstream_FaultHubException_Context() {
            var failingInnerStream = Observable.Throw<Unit>(new InvalidOperationException("Original failure"))
                                             .ChainFaultContext(["UpstreamContext"]);

            var resilientStream = this.DeferItemResilient(() => failingInnerStream, context: ["DownstreamContext"]);

            await resilientStream.Capture();

            BusEvents.Count.ShouldBe(1);
            var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();

            fault.AllContexts.ShouldContain("UpstreamContext", "The upstream context from ChainFaultContext was lost.");
            fault.AllContexts.ShouldNotContain("DownstreamContext", "The downstream operator incorrectly used the new context for the wrapper.");
        }

        [Test]
        public async Task UsingItemResilient_Preserves_Upstream_FaultHubException_Context() {
            var failingInnerStream = Observable.Throw<Unit>(new InvalidOperationException("Original failure"))
                                             .ChainFaultContext(["UpstreamContext"]);

            var resilientStream = this.UsingItemResilient(() => System.Reactive.Disposables.Disposable.Empty, _ => failingInnerStream, context: ["DownstreamContext"]);

            await resilientStream.Capture();

            BusEvents.Count.ShouldBe(1);
            var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();

            fault.AllContexts.ShouldContain("UpstreamContext", "The upstream context from ChainFaultContext was lost.");
            fault.AllContexts.ShouldNotContain("DownstreamContext", "The downstream operator incorrectly used the new context for the wrapper.");
        }
        
        [Test]
        public async Task SelectManySequentialItemResilient_Preserves_Context_And_Stack() {
            var source = Observable.Return("Input");
            
            var result = await source.SelectManySequentialItemResilient(s => 
                    Observable.Throw<string>(new InvalidOperationException("Sequential Failure"))
                        .PushStackFrame("InnerFrame"), 
                context: ["OuterContext"]).Capture();

            result.IsCompleted.ShouldBeTrue();
            BusEvents.Count.ShouldBe(1);

            var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            
            // Verify inner stack frame (PushStackFrame)
            fault.LogicalStackTrace.ShouldContain(f => f.MemberName == "InnerFrame");
            
            // Verify outer context (SelectManySequentialItemResilient context parameter)
            // This confirms the SafeguardSubscription fix is working
            fault.AllContexts.ShouldContain("OuterContext");
        }
    }
}