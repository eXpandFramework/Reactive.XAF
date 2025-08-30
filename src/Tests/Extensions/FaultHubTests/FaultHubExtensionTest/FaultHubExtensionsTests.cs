using System;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;

namespace Xpand.Extensions.Tests.FaultHubTests.FaultHubExtensionTest {
    public class FaultHubExtensionsRenderTests : FaultHubExtensionTestBase {
        private FaultHubException CreateProductionScenarioException() {
            var upcomingEx = new InvalidOperationException("Upcoming");
            var ctxWebSite = new AmbientFaultContext { BoundaryName = "WebSiteUrls" };
            var fhWebSite = new FaultHubException("WebSiteUrls failed", upcomingEx, ctxWebSite);
            var ctxWhenUpcoming = new AmbientFaultContext { BoundaryName = "WhenUpcomingUrls", InnerContext = fhWebSite.Context };
            var fhWhenUpcoming = new FaultHubException("WhenUpcomingUrls failed", fhWebSite, ctxWhenUpcoming);

            var startParsingEx = new InvalidOperationException("StartParsing");
            var ctxStartParsing = new AmbientFaultContext { BoundaryName = "StartParsing" };
            var fhStartParsing = new FaultHubException("StartParsing failed", startParsingEx, ctxStartParsing);
            var ctxProjectTx = new AmbientFaultContext { BoundaryName = "ProjectParseTransaction", InnerContext = fhStartParsing.Context };
            var fhProjectTx = new FaultHubException("ProjectParseTransaction failed", fhStartParsing, ctxProjectTx);
            var ctxWhenExisting = new AmbientFaultContext { BoundaryName = "WhenExistingProjectPageParsed", InnerContext = fhProjectTx.Context };
            var fhWhenExisting = new FaultHubException("WhenExisting... failed", fhProjectTx, ctxWhenExisting);
            var ctxParseProjects = new AmbientFaultContext { BoundaryName = "ParseUpcomingProjects", InnerContext = fhWhenExisting.Context };
            var fhParseProjects = new FaultHubException("ParseUpcomingProjects failed", fhWhenExisting, ctxParseProjects);

            var aggEx = new AggregateException(fhWhenUpcoming, fhParseProjects);
            var ctxParseUpcoming = new AmbientFaultContext { BoundaryName = "ParseUpComing" };
            var fhParseUpcoming = new FaultHubException("ParseUpComing failed", aggEx, ctxParseUpcoming);
            var ctxConnect = new AmbientFaultContext { BoundaryName = "ConnectLaunchPad", InnerContext = fhParseUpcoming.Context };
            var fhConnect = new FaultHubException("Connect... failed", fhParseUpcoming, ctxConnect);
            var ctxParseLaunchPad = new AmbientFaultContext { BoundaryName = "ParseLaunchPad", InnerContext = fhConnect.Context };
            var fhParseLaunchPad = new FaultHubException("ParseLaunchPad failed", fhConnect, ctxParseLaunchPad);
            var ctxSchedule = new AmbientFaultContext { BoundaryName = "ScheduleLaunchPadParse", InnerContext = fhParseLaunchPad.Context };
            
            return new FaultHubException("ScheduleLaunchPadParse failed", fhParseLaunchPad, ctxSchedule);
        }

        [Test]
        public void Render_Correctly_Formats_Production_Scenario_With_Multiple_Failures() {
            var exception = CreateProductionScenarioException();
            
            var expected = string.Join(Environment.NewLine,
                "Schedule Launch Pad Parse completed with errors (2 times: Upcoming • StartParsing)",
                "└ Parse Launch Pad",
                "  └ Connect Launch Pad",
                "    └ Parse Up Coming",
                "      ├ When Upcoming Urls",
                "      │ └ Web Site Urls",
                "      │   • Root Cause: System.InvalidOperationException: Upcoming",
                "      │     --- Original Exception Details ---",
                "      │       System.InvalidOperationException: Upcoming",
                "      └ Parse Upcoming Projects",
                "        └ When Existing Project Page Parsed",
                "          └ Project Parse Transaction",
                "            └ Start Parsing",
                "              • Root Cause: System.InvalidOperationException: StartParsing",
                "                --- Original Exception Details ---",
                "                  System.InvalidOperationException: StartParsing"
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
                "Outer Operation completed with errors (1 times: Root Cause)",
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
            report.ShouldContain("Test Operation completed with errors (1 times: Root Cause)");
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
        
        [TestCase("Schedule Launch Pad Parse")]
        [TestCase("ScheduleLaunchPadParse")]
        public void Render_Removes_Redundant_BoundaryName_From_Root_Context(string context) {
            var innerEx = new InvalidOperationException("Root Cause");
            var innerCtx = new AmbientFaultContext { BoundaryName = "InnerOperation" };
            var fhInner = new FaultHubException("Inner fail", innerEx, innerCtx);

            var outerCtx = new AmbientFaultContext {
                BoundaryName = "Schedule Launch Pad Parse",
                UserContext = [context, "Kommunitas Kommunitas"],
                InnerContext = fhInner.Context
            };
            var exception = new FaultHubException("Outer fail", fhInner, outerCtx);

            var expectedHeaderStart = "Schedule Launch Pad Parse completed with errors [Kommunitas Kommunitas]";

            var result = exception.Render();

            result.ShouldStartWith(expectedHeaderStart);
            result.ShouldNotContain($"[{context}, Kommunitas Kommunitas]");
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
        
    }
}