using System;
using System.Linq;
using System.Net.Http;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Numeric;
using Xpand.Extensions.Reactive.ErrorHandling;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Tests.FaultHubTests.Diagnostics{
    [TestFixture]
    public class DependentTransactionTests : FaultHubTestBase {
        private IObservable<Unit> OperationA() 
            => Observable.Defer(() => Observable.Throw<Unit>(new InvalidOperationException("Operation A Failed")))
                ;
        private IObservable<Unit> VisitPage(string url) 
            => Observable.Defer(() => url.Contains("page2")//THROWS for Page2
                ? Observable.Throw<Unit>(new HttpRequestException("Failed to load page2"))
                : Unit.Default.Observe());
        
        [Test][Apartment(ApartmentState.STA)]
        public async Task Successful_Dependent_Transaction_With_Resilience() {
            var transaction = NavigateToHomePage()
                .BeginWorkflow("LoginProcess")
                .Then(_ => Authenticate())
                .Then(_ => ParseData())
                .Then(strings => ProcessAllUrlsSequentially(strings)) 
                .RunFailFast();

            var captureResult = await transaction.Capture();
            
            Clipboard.SetText(captureResult.Error.ToString());
            Console.WriteLine(captureResult.Error);
            captureResult.Error.ShouldNotBeNull();

            var transactionAbortedException = captureResult.Error.ShouldBeOfType<TransactionAbortedException>();
            
            transactionAbortedException.FindRootCauses().Count().ShouldBe(2);
        }

        private IObservable<Unit> NavigateToHomePage() => Observable.Defer(() => Unit.Default.Observe());
        private IObservable<Unit> Authenticate() => Unit.Default.Observe();
        private IObservable<string> ParseData() => Observable.Defer(() => new[] { "http://example.com/page1", "http://example.com/page2" }.ToNowObservable());
        private IObservable<Unit> OperationB() => Observable.Defer(() => Unit.Default.Observe());
        private IObservable<Unit> VisitAndProcessUrl(string url) {
            var processOperations = OperationA();
                

            return VisitPage(url)
                .BeginWorkflow($"Visit-{url}") 
                .Then(_ => processOperations)
                .RunFailFast() // If visit fails, abort this sub-transaction.
                .ToUnit();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit[]> ProcessAllUrlsSequentially(string[] urls) {
            var operations = urls.Select(VisitAndProcessUrl).ToArray();
            if (!operations.Any()) {
                return Observable.Return(Array.Empty<Unit>());
            }

            var initialBuilder = operations.First().BeginWorkflow();
            var finalBuilder = operations.Skip(1).Aggregate(initialBuilder, (builder, step) => builder.Then(step));

            return finalBuilder.RunToEnd();
        }
    }
}