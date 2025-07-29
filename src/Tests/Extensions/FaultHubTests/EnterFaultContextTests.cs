using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using akarnokd.reactive_extensions;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Numeric;
using Xpand.Extensions.Reactive.ErrorHandling;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Tests.FaultHubTests{
    [TestFixture]
    public class EnterFaultContextTests : FaultHubTestBase {
        [Test]
        public async Task AsyncLocal_Is_Not_Lost_By_Timeout() {
            var testContext = new AmbientFaultContext();
            FaultHub.CurrentContext.Value = testContext;
            
            this.Defer(() => Observable.Never<Unit>().Timeout(TimeSpan.FromMilliseconds(20))).WithFaultContext().Test();

            await Task.Delay(500.Milliseconds());
            FaultHub.CurrentContext.Value = null;

            BusObserver.ItemCount.ShouldBe(1);
            var publishedException = BusObserver.Items.Single();
            
            publishedException.ShouldBeOfType<FaultHubException>();
        }

        [Test]
        public void Captures_Context_For_Simple_Resilient_Stream() {
            using var _ = this.Defer(() => Observable.Throw<Unit>(new InvalidOperationException("Error")).WithFaultContext(["SimpleContext"]))
                
                .Test();

            BusObserver.ItemCount.ShouldBe(1);
            var ex = BusObserver.Items.Single().ShouldBeOfType<FaultHubException>();
            ex.Context.CustomContext.ShouldContain("SimpleContext");
            ex.InnerException.ShouldBeOfType<InvalidOperationException>();
        }
        
        [Test]
        public void Handles_Nested_Contexts_Correctly() {
            this.Defer(() => 
                Unit.Default.Observe()
                    .SelectMany(_ =>
                        Observable.Throw<Unit>(new Exception("Nested Error"))
                            .WithFaultContext(["Inner"])
                    )
                    .WithFaultContext(["Outer"])
            ).Test();

            BusObserver.ItemCount.ShouldBe(1);
            var ex = BusObserver.Items.Single().ShouldBeOfType<FaultHubException>();
            
            var contexts = ex.Context.CustomContext.JoinCommaSpace();
            contexts.ShouldContain("Inner");
            contexts.ShouldContain("Outer");
        }
        [Test]
        public void Handles_Nested_Contexts_Correctly1() {
            this.Defer(() => Unit.Default.Observe()
                    .SelectMany(_ => Observable.Throw<Unit>(new Exception("Nested Error")).WithFaultContext(["Outer"])))
                    
                .Test();

            BusObserver.ItemCount.ShouldBe(1);
            var ex = BusObserver.Items.Single().ShouldBeOfType<FaultHubException>();
            
            ex.Context.CustomContext.ShouldContain("Outer");
        }
        [Test]
        public void Handles_Nested_Contexts_Correctly2() {
            this.Defer(() => Unit.Default.Observe()
                    .SelectManyResilient(_ => Observable.Throw<Unit>(new Exception("Nested Error"))).WithFaultContext(["Outer"])
            ).Test();

            BusObserver.ItemCount.ShouldBe(1);
            var ex = BusObserver.Items.Single().ShouldBeOfType<FaultHubException>();
            
            ex.Context.CustomContext.ShouldContain("Outer");
        }
        
        [Test]
        public async Task Works_With_Retry_Logic() {
            
            var attemptCount = 0;
            var retrySelector = (Func<IObservable<Unit>, IObservable<Unit>>)(source => source.RetryWithBackoff(3,_ => 100.ToMilliseconds()));

            using var _ = this.Defer(() => {
                        attemptCount++;
                        return Observable.Throw<Unit>(new InvalidOperationException("Retry Error"))
                            ;
                    }
                ).WithFaultContext(["RetryContext"],retrySelector)
                
                .Test();

            await Task.Delay(1.ToSeconds());
            attemptCount.ShouldBe(3);
            BusObserver.ItemCount.ShouldBe(1);
            var ex = BusObserver.Items.Single().ShouldBeOfType<FaultHubException>();
            ex.Context.CustomContext.ShouldContain("RetryContext");
            ex.Context.CustomContext.Join().ShouldContain("Defer");
        }
        

    }
}