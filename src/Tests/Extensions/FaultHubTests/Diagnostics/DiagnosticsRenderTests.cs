using System;
using System.Linq;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;
using Xpand.Extensions.Tests.FaultHubTests.FaultHubExtensionTest;

namespace Xpand.Extensions.Tests.FaultHubTests.Diagnostics {
    public class DiagnosticsRenderTests  : FaultHubExtensionTestBase {
        private FaultHubException CreateWebScrapingScenarioException() {
            var upcomingEx = new InvalidOperationException("Failed to fetch URLs");
            var ctxWebSite = new AmbientFaultContext { BoundaryName = "FetchInitialUrls" };
            var fhWebSite = new FaultHubException("FetchInitialUrls failed", upcomingEx, ctxWebSite);
            var ctxWhenUpcoming = new AmbientFaultContext { BoundaryName = "GetPageLinks", InnerContext = fhWebSite.Context };
            var fhWhenUpcoming = new FaultHubException("GetPageLinks failed", fhWebSite, ctxWhenUpcoming);

            var startParsingEx = new InvalidOperationException("Failed to extract content");
            var ctxStartParsing = new AmbientFaultContext { BoundaryName = "ExtractContent" };
            var fhStartParsing = new FaultHubException("ExtractContent failed", startParsingEx, ctxStartParsing);
            var ctxProjectTx = new AmbientFaultContext { BoundaryName = "DataExtractionTransaction", InnerContext = fhStartParsing.Context };
            var fhProjectTx = new FaultHubException("DataExtractionTransaction failed", fhStartParsing, ctxProjectTx);
            var ctxWhenExisting = new AmbientFaultContext { BoundaryName = "WhenLinkScraped", InnerContext = fhProjectTx.Context };
            var fhWhenExisting = new FaultHubException("WhenLinkScraped... failed", fhProjectTx, ctxWhenExisting);
            var ctxParseProjects = new AmbientFaultContext { BoundaryName = "ScrapeDataFromLinks", InnerContext = fhWhenExisting.Context };
            var fhParseProjects = new FaultHubException("ScrapeDataFromLinks failed", fhWhenExisting, ctxParseProjects);

            var aggEx = new AggregateException(fhWhenUpcoming, fhParseProjects);
            var ctxParseUpcoming = new AmbientFaultContext { BoundaryName = "ExtractAndProcessLinks" };
            var fhParseUpcoming = new FaultHubException("ExtractAndProcessLinks failed", aggEx, ctxParseUpcoming);
            var ctxConnect = new AmbientFaultContext { BoundaryName = "NavigateToPage", InnerContext = fhParseUpcoming.Context };
            var fhConnect = new FaultHubException("Navigate... failed", fhParseUpcoming, ctxConnect);
            var ctxParseLaunchPad = new AmbientFaultContext { BoundaryName = "ParseHomePage", InnerContext = fhConnect.Context };
            var fhParseLaunchPad = new FaultHubException("ParseHomePage failed", fhConnect, ctxParseLaunchPad);
            var ctxSchedule = new AmbientFaultContext { BoundaryName = "ScheduleWebScraping", InnerContext = fhParseLaunchPad.Context };
            return new FaultHubException("ScheduleWebScraping failed", fhParseLaunchPad, ctxSchedule);
        }

        [Test]
        public void Render_Correctly_Formats_Web_Scraping_Scenario_With_Multiple_Failures() {
            var exception = CreateWebScrapingScenarioException();
            var expected = string.Join(Environment.NewLine,
                "Schedule Web Scraping completed with errors (2 times: Failed to fetch URLs • Failed to extract content)",
                "└ Parse Home Page",
                "  └ Navigate To Page",
                "    └ Extract And Process Links",
       
                     "      ├ Get Page Links",
                "      │ └ Fetch Initial Urls",
                "      │   • Root Cause: System.InvalidOperationException: Failed to fetch URLs",
                "      │     --- Original Exception Details ---",
                "      │       System.InvalidOperationException: Failed to fetch URLs",
                "      └ Scrape Data From Links",
                "        └ When Link Scraped",
            
                 "          └ Data Extraction Transaction",
                "            └ Extract Content",
                "              • Root Cause: System.InvalidOperationException: Failed to extract content",
                "                --- Original Exception Details ---",
                "                  System.InvalidOperationException: Failed to extract content"
            );
            var result = exception.Render();

            var resultWithoutStackTrace = System.Text.RegularExpressions.Regex.Replace(result, @"at .*", "...");
            var expectedWithoutStackTrace = System.Text.RegularExpressions.Regex.Replace(expected, @"at .*", "...");

            resultWithoutStackTrace.ShouldBe(expectedWithoutStackTrace);
        }

        [Test]
        public void Render_Correctly_Formats_Single_Failure_Path() {
            var innerEx = new InvalidOperationException("Root Cause");
            var innerCtx = new AmbientFaultContext { BoundaryName = "InnerOperation" };
            var fhInner = new FaultHubException("Inner fail", innerEx, innerCtx);
            var outerCtx = new AmbientFaultContext { BoundaryName = "OuterOperation", InnerContext = fhInner.Context };
            var exception = new FaultHubException("Outer fail", fhInner, outerCtx);
            
            var expected = string.Join(Environment.NewLine,
                "Outer Operation completed with errors (Root Cause)",
                "└ Inner Operation",
                "  • Root Cause: System.InvalidOperationException: Root Cause",
                "    --- Original Exception Details ---",
  
                               "      System.InvalidOperationException: Root Cause"
            );
            var result = exception.Render();

            var resultWithoutStackTrace = System.Text.RegularExpressions.Regex.Replace(result, @"at .*", "...");
            var expectedWithoutStackTrace = System.Text.RegularExpressions.Regex.Replace(expected, @"at .*", "...");
            resultWithoutStackTrace.ShouldBe(expectedWithoutStackTrace);
        }
        
        [Test]
        public void Render_Correctly_Formats_Result_Of_Union() {
            var path1 = new OperationNode("RootNode", [], [new OperationNode("CommonChild", [], [new OperationNode("LeafA", [], [])])]);
            var path2 = new OperationNode("RootNode", [], [new OperationNode("CommonChild", [], [new OperationNode("LeafB", [], [])])]);
            var path3 = new OperationNode("RootNode", [], [new OperationNode("DifferentChild", [], [])]);
            var source = new[] { path1, path2, path3 };
            var expected = string.Join(Environment.NewLine,
                "└ Root Node",
                "  ├ Common Child",
                "  │ ├ Leaf A",
                "  │ └ Leaf B",
                "  └ Different Child"
            );

            var unionedNode = source.Union();
            var result = unionedNode.Render();

            result.ShouldBe(expected);
        }
        private FaultHubException CreateFaultWithLogicalStack(params LogicalStackFrame[] frames) {
            var context = new AmbientFaultContext {
                BoundaryName = "TestOperation",
                LogicalStackTrace = frames
            };
            return new FaultHubException("Test Failure", new InvalidOperationException("Root Cause"), context);
        }
        [Test]
        public void Render_Displays_Details_For_Childless_Root_Node() {
            var exception = CreateFaultWithLogicalStack(
                new LogicalStackFrame("MyMethod", @"C:\app\logic.cs", 10)
            );
            var report = exception.Render();
            Console.WriteLine(report);
            report.ShouldContain("Test Operation completed with errors (Root Cause)");
            report.ShouldContain("MyMethod");
            report.ShouldContain("--- Invocation Stack ---");
            report.ShouldContain("• Root Cause: System.InvalidOperationException: Root Cause");
        }
        
        [Test]
        public void Render_Correctly_Handles_Top_Level_Exception_Wrapping_An_AggregateException() {
            var leafEx = new InvalidOperationException("Root Cause");
            var leafCtx = new AmbientFaultContext { BoundaryName = "LeafOperation" };
            var fhLeaf = new FaultHubException("Leaf fail", leafEx, leafCtx);
            var aggEx = new AggregateException(fhLeaf);

            var topCtx = new AmbientFaultContext { BoundaryName = "TopLevelOperation" };
            var fhTop = new FaultHubException("Top-level failure message", aggEx, topCtx);

            var report = fhTop.Render();

            report.ShouldStartWith("Top Level Operation completed with errors");
            report.ShouldContain("└ Leaf Operation");
            report.ShouldContain("• Root Cause: System.InvalidOperationException: Root Cause");
        }
        
        [TestCase("Schedule Web Scraping")]
        [TestCase("ScheduleWebScraping")]
        public void Render_Removes_Redundant_BoundaryName_From_Root_Context(string context) {
            var innerEx = new InvalidOperationException("Root Cause");
            var innerCtx = new AmbientFaultContext { BoundaryName = "InnerOperation" };
            var fhInner = new FaultHubException("Inner fail", innerEx, innerCtx);
            var outerCtx = new AmbientFaultContext {
                BoundaryName = "Schedule Web Scraping",
                UserContext = [context, "Example.com Scrape"],
                InnerContext = fhInner.Context
            };
            var exception = new FaultHubException("Outer fail", fhInner, outerCtx);

            var expectedHeaderStart = "Schedule Web Scraping completed with errors [Example.com Scrape]";
            var result = exception.Render();

            result.ShouldStartWith(expectedHeaderStart);
            result.ShouldNotContain($"[{context}, Example.com Scrape]");
        }
        
        [Test]
        public void Render_Removes_Redundant_Context_From_Node_With_Method_Signature_BoundaryName() {
            var rootCause = new InvalidOperationException("Failure");
            var childCtx = new AmbientFaultContext {
                BoundaryName = "MyMethod(string arg1, int arg2)",
                UserContext = ["MyMethod"]
            };
            var fhChild = new FaultHubException("Child fail", rootCause, childCtx);

            var rootCtx = new AmbientFaultContext {
                BoundaryName = "RootOperation",
                InnerContext = fhChild.Context
            };
            var exception = new FaultHubException("Root fail", fhChild, rootCtx);

            var result = exception.Render();

            result.ShouldStartWith("Root Operation completed with errors");

            result.ShouldContain("└ My Method");
            result.ShouldNotContain("(MyMethod)");
        }
    
        [Test]
        public void Render_Correctly_Applies_Prefixes_And_Filters_Tags_From_Context() {
            var stepEx = new InvalidOperationException("Step Failure");
            var stepCtx = new AmbientFaultContext {
                BoundaryName = "MyNestedStep",
                UserContext = ["Step", "SomeStepData"],
                Tags = ["Step"]
            };
            var fhStep = new FaultHubException("Step failed", stepEx, stepCtx);

            var txCtx = new AmbientFaultContext {
                BoundaryName = "MyTransaction",
                UserContext = ["Transaction", "RunToEnd", "SomeTxData"],
                Tags = ["Transaction", "RunToEnd"],
                InnerContext = fhStep.Context
            
            };
            var fhTx = new FaultHubException("Transaction failed", fhStep, txCtx);

            var report = fhTx.Render();
            var reportLines = report.Split([Environment.NewLine], StringSplitOptions.None);
            var txLine = reportLines.FirstOrDefault(l => l.Contains("My Transaction"));
            txLine.ShouldNotBeNull();
            txLine.Trim().ShouldStartWith("Transaction RunToEnd: My Transaction");
            txLine.ShouldContain("[SomeTxData]");
            var stepLine = reportLines.FirstOrDefault(l => l.Contains("My Nested Step"));
            stepLine.ShouldNotBeNull();
            stepLine.Trim().ShouldContain("Step: My Nested Step");
            stepLine.ShouldContain("(SomeStepData)");
        }
        
        [TestCase(1, true, TestName = "Render_Uses_Simplified_Summary_For_Single_Failure")]
        [TestCase(2, true, TestName = "Render_Shows_Full_Summary_For_Multiple_Failures")]
        public void Render_Correctly_Formats_Error_Summary(int errorCount, bool shouldShowSummary) {
            FaultHubException exception;
            if (errorCount == 1) {
                exception = CreateNestedFault(("SinglePath", null));
            }
            else {
                var exA = new InvalidOperationException("Failure A");
                var fhA = new FaultHubException("A failed", exA, new AmbientFaultContext { BoundaryName = "BranchA" });
        
                var exB = new InvalidOperationException("Failure B");
                var fhB = new FaultHubException("B failed", exB, new AmbientFaultContext { BoundaryName = "BranchB" });

                var aggEx = new AggregateException(fhA, fhB);
                exception = new FaultHubException("Root failed", aggEx, new AmbientFaultContext { BoundaryName = "RootOperation" });
            }

            var result = exception.Render();
            if (errorCount == 1) {
                result.ShouldContain("(Innermost failure)");
                result.ShouldNotContain("1 times:");
            }
            else if (shouldShowSummary) {
                var summaryText = $"{errorCount} times:";
                result.ShouldContain(summaryText);
            }
            else {
                result.ShouldNotContain("times:");
                result.ShouldContain(exception.ErrorStatus);
            }
        }
    
        
    }
}