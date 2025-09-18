using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.Relay;
using Xpand.Extensions.Reactive.Relay.Transaction;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Tests.FaultHubTests.Diagnostics {
    [TestFixture]
    public class ProductionScenario2Tests:FaultHubTestBase {

        #region Operation Simulation

        private IObservable<Unit> ExtractContent()
            => Observable.Throw<Unit>(new Exception("Failed to extract content"));
        private IObservable<Unit> DataExtractionTransaction()
            => Unit.Default.Observe().BeginWorkflow("Data Extraction Transaction")
                .Then(_ => ExtractContent(), "Extract Content")
                .RunToEnd().ToUnit();
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        [SuppressMessage("ReSharper", "UnusedParameter.Local")]
        private IObservable<Unit> WhenLinkScraped(string someNoisyArgument)
            => Unit.Default.Observe().BeginWorkflow("When Link Scraped")
                .Then(_ => DataExtractionTransaction())
                .RunToEnd().ToUnit();
        private IObservable<Unit> ScrapeDataFromLinks()
            => Unit.Default.Observe().BeginWorkflow("Scrape Data From Links")
                .Then(_ => WhenLinkScraped("some noisy argument"))
                .RunToEnd().ToUnit();
        private IObservable<Unit> GetPageLinks()
            => Observable.Throw<Unit>(new Exception("Failed to get links")) ;
        private IObservable<Unit> ExtractAndProcessLinks()
            => Unit.Default.Observe().BeginWorkflow("Extract And Process Links")
                .Then(_ => GetPageLinks())
                .Then(_ => ScrapeDataFromLinks())
                .RunToEnd().ToUnit();
        private IObservable<Unit> ScheduleWebScraping()
            => Unit.Default.Observe().BeginWorkflow("Schedule Web Scraping")
                .Then(_ => ExtractAndProcessLinks())
                .RunToEnd().ToUnit();
        #endregion
        
        [Test][Apartment(ApartmentState.STA)]
        public async Task Generates_Concise_Execution_Report_For_Web_Scraping_Scenario() {
            
            await ScheduleWebScraping().PublishFaults().Capture();
            BusEvents.Count.ShouldBe(1);
            var finalReport = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            
            AssertFaultExceptionReport(finalReport);
        }
        
        [Test][Apartment(ApartmentState.STA)]
        public void Traverses_Parses_And_Renders_Complex_Nested_Exception_Correctly() {
            #region ARRANGE
            var ctxSchedule = new AmbientFaultContext {
                BoundaryName = "ScheduleWebScraping()",
                UserContext = ["Schedule Web Scraping", "Example.com"]
            };
            var ctxExtractAndProcess = new AmbientFaultContext
                { BoundaryName = "ExtractAndProcessLinks", UserContext = ["Extract And Process Links", "Main Site"] };
            var ctxGetPageLinks = new AmbientFaultContext {
                BoundaryName = "GetPageLinks(scraperService, browser, page)",
                UserContext = ["Get Page Links"],
                LogicalStackTrace =
                    [new LogicalStackFrame("GetPageLinks", @"Services\WebScrapingService.cs", 582)]
            };
            var ctxScrapeData = new AmbientFaultContext {
                BoundaryName = "ScrapeDataFromLinks()",
                UserContext = ["Scrape Data From Links", "Main Site"]
            };
            var ctxWhenLinkScraped = new AmbientFaultContext
                { BoundaryName = "WhenLinkScraped", UserContext = ["When Link Scraped"] };
            var ctxDataExtractionTx = new AmbientFaultContext
                { BoundaryName = "DataExtractionTransaction()", UserContext = ["Data Extraction Transaction"] };
            var ctxExtractContent = new AmbientFaultContext {
                BoundaryName = "ExtractContent(browser, pageContent)",
                UserContext = ["Extract Content", "browser", "pageContent"],
                LogicalStackTrace =
                    [new LogicalStackFrame("ExtractContent", @"Services\WebScrapingService.cs", 1228, "MyDynamicValue")]
            };
            var rootCause1 = new Exception("Failed to get links");
            var fh1GetLinks = new FaultHubException("Failed to get links", rootCause1, ctxGetPageLinks);

            var rootCause2 = new Exception("Failed to extract content");
            var fh2ExtractContent = new FaultHubException("Failed to extract content", rootCause2, ctxExtractContent);
            var agg21 = new AggregateException(fh2ExtractContent);
            var fh2DataExtractionTx = new FaultHubException("Data Extraction Transaction completed with errors", agg21, ctxDataExtractionTx);
            var agg22 = new AggregateException(fh2DataExtractionTx);
            var fh2WhenLinkScraped =
                new FaultHubException("When Link Scraped completed with errors", agg22, ctxWhenLinkScraped);
            var agg23 = new AggregateException(fh2WhenLinkScraped);
            var fh2ScrapeData = new FaultHubException("Scrape Data From Links completed with errors", agg23, ctxScrapeData);
            var aggMid = new AggregateException(fh1GetLinks, fh2ScrapeData);
            var fhExtractAndProcess = new FaultHubException("Extract And Process Links completed with errors", aggMid, ctxExtractAndProcess);
            var aggTop = new AggregateException(fhExtractAndProcess);
            var topLevelException = new FaultHubException("Schedule Web Scraping completed with errors", aggTop, ctxSchedule);
            #endregion
            
            AssertFaultExceptionReport(topLevelException);
        }
        
        [Test][Apartment(ApartmentState.STA)]
        public async Task Report_Aggregates_Context_From_Nested_ChainFaultContext_Boundaries() {
            var innermostOperation = Observable.Throw<Unit>(new InvalidOperationException("Deep Failure"))
                .ChainFaultContext(["InnermostContext"]);
            var intermediateOperation = innermostOperation
                .ChainFaultContext(["IntermediateContext"]);
            var outermostOperation = intermediateOperation
                .ChainFaultContext(["OutermostContext"]);
            await outermostOperation.PublishFaults().Capture();

            BusEvents.Count.ShouldBe(1);
            var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();

            var report = fault.ToString();
            Console.WriteLine(report);
            Clipboard.SetText(report);
            
            report.ShouldContain("OutermostContext");
            report.ShouldContain("IntermediateContext");
            report.ShouldContain("InnermostContext");

            report.ShouldContain("Deep Failure");
        }

        
    }
}