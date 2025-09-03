using System;
using System.Linq;
using System.Net.Http;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Numeric;
using Xpand.Extensions.Reactive.ErrorHandling;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Tests.FaultHubTests.Diagnostics{
    [TestFixture]
public class DependentTransactionTests {
    private IObservable<Unit> OperationA() 
        => Observable.Throw<Unit>(new InvalidOperationException("Operation A Failed")).RetryWithBackoff(2); //THROWS;
    private IObservable<Unit> VisitPage(string url) 
        => Observable.Defer(() => url.Contains("page1")//THROWS for Page1
            ? Observable.Throw<Unit>(new HttpRequestException("Failed to load page1"))
            : Unit.Default.Observe()).RetryWithBackoff(2, _ => 1.Milliseconds());
    [Test][Apartment(ApartmentState.STA)]
    public async Task Nested_RunToEnd_With_Failures_Aborts_Outer_FailFast_Transaction() {
        var transaction = NavigateToHomePage()
            .BeginWorkflow("LoginProcess")
            .Then(_ => Authenticate())
            .Then(_ => ParseData())
            .Then(ProcessAllUrlsSequentially) 
            .RunFailFast();

        var captureResult = await transaction.Capture();

        var exception = captureResult.Error;
        Console.WriteLine(exception);
        Clipboard.SetText(exception.ToString());
        var transactionAbortedException = exception.ShouldBeOfType<TransactionAbortedException>();

        transactionAbortedException.FindRootCauses().Count().ShouldBe(2);
    }

    private IObservable<Unit> NavigateToHomePage() => Observable.Defer(() => Unit.Default.Observe()).RetryWithBackoff(2, _ => 1.Seconds());
    private IObservable<Unit> Authenticate() => Unit.Default.Observe();
    private IObservable<string> ParseData() => Observable.Defer(() => new[] { "http://example.com/page1", "http://example.com/page2" }.ToNowObservable());
    private IObservable<Unit> OperationB() => Observable.Defer(() => Unit.Default.Observe()).RetryWithBackoff(2);
    private IObservable<Unit> VisitAndProcessUrl(string url) {
        var processOperations = new [] { OperationA(), OperationB() }
            .BeginWorkflow($"Processing-{url}", TransactionMode.Concurrent)
            .RunToEnd(); // Run A & B, continue even if one fails, but aggregate the error.

        return VisitPage(url)
            .BeginWorkflow($"Visit-{url}") 
            .Then(_ => processOperations)
            .RunFailFast() // If visit fails, abort this sub-transaction.
            .ToUnit();
    }

    private IObservable<object[]> ProcessAllUrlsSequentially(string[] urls) 
        => urls.Select(VisitAndProcessUrl)
            .BeginWorkflow() //transaction name taken from method name == ProcessAllUrlsSequentially
            .RunToEnd(); //all urls are processed

}}