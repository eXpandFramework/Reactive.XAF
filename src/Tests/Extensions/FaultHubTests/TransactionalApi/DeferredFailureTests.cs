using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Tests.FaultHubTests.TransactionalApi {
    [TestFixture]
    public class DeferredFailureTests:FaultHubTestBase {
        public class TestableExternalService {
            public bool EssentialStepWasExecuted { get; private set; }

            public IObservable<Unit> WhenVisitPage(Uri uri) {
                return MockHttp(uri)
                    .SelectMany(html => new[] {
                        MockParsePage(html).AsStep(),
                        FlakyOperationThatCanFail(html).AsStep(),
                        AnotherEssentialStep(html).AsStep()
                    }.AsStep("ExecutingAllSteps"));
            }

            
            [SuppressMessage("ReSharper", "UnusedParameter.Global")]
            public IObservable<Unit> AnotherEssentialStep(string html) {
                return Observable.FromAsync(() => {
                    EssentialStepWasExecuted = true;
                    return Task.CompletedTask;
                });
            }
            [SuppressMessage("ReSharper", "UnusedParameter.Global")]
            internal IObservable<Unit> FlakyOperationThatCanFail(string html) 
                => Observable.Throw<Unit>(new InvalidOperationException("This is a deferred failure."));

            [SuppressMessage("ReSharper", "UnusedParameter.Local")]
            private IObservable<string> MockHttp(Uri uri) => Observable.Return("<html>...</html>");
            [SuppressMessage("ReSharper", "UnusedParameter.Local")]
            private IObservable<Unit> MockParsePage(string html) => Observable.Return(Unit.Default);
        }

        [Test]
        public async Task Deferred_Failure_Allows_Sibling_Operations_To_Complete_Before_Failing() {
            var service = new TestableExternalService();

            await service.WhenVisitPage(new Uri("http://example.com"))
                .BeginWorkflow()
                .RunFailFast()
                .PublishFaults()
                .Capture();

            service.EssentialStepWasExecuted.ShouldBe(true);

            BusEvents.Count.ShouldBe(1);
            var finalException = BusEvents.Single().ShouldBeOfType<TransactionAbortedException>();
            
            var deferredException = finalException.InnerException.ShouldBeOfType<FaultHubException>();
            
            deferredException.Context.ShouldNotBeNull();
            deferredException.Context.Name.ShouldBe(nameof(TestableExternalService.FlakyOperationThatCanFail));
            
            var originalException = deferredException.InnerException.ShouldBeOfType<InvalidOperationException>();
            originalException.Message.ShouldBe("This is a deferred failure.");
            
        }
    }
}