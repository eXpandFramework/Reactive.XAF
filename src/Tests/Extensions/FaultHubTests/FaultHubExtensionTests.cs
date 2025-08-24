using System;
using System.Collections.Generic;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;

namespace Xpand.Extensions.Tests.FaultHubTests {
    [TestFixture]
    public class FaultHubExtensionTests {

        [Test]
        public void Traverses_Parses_And_Renders_Complex_Nested_Exception_Correctly() {
            #region ARRANGE
            #region ARRANGE

                var ctxSchedule = new AmbientFaultContext { BoundaryName = "ScheduleLaunchPadParse()", UserContext = ["Schedule Launch Pad Parse", "Kommunitas Kommunitas"] };
    var ctxParseUpcoming = new AmbientFaultContext { BoundaryName = "ParseUpComing", UserContext = ["Parse Up Coming", "LaunchPad kommunitas"] };
    var ctxWhenUpcoming = new AmbientFaultContext { BoundaryName = "WhenUpcomingUrls(serviceModule, driver, launchPad)", UserContext = ["When Upcoming Urls"], LogicalStackTrace = [new("WhenUpcomingUrls", @"Services\LaunchPadProjectPageParseService.cs", 582)] };
    var ctxParseProjects = new AmbientFaultContext { BoundaryName = "ParseUpcomingProjects()", UserContext = ["Parse Upcoming Projects", "LaunchPad kommunitas"] };
    var ctxWhenExisting = new AmbientFaultContext { BoundaryName = "WhenExistingProjectPageParsed", UserContext = ["When Existing Project Page Parsed"] };
    var ctxParseTx = new AmbientFaultContext { BoundaryName = "ProjectParseTransaction()", UserContext = ["Project Parse Transaction"] };
    var ctxStartParsing = new AmbientFaultContext { BoundaryName = "StartParsing(driver, padProject)", UserContext = ["Start Parsing", "driver", "padProject"], LogicalStackTrace = [new("StartParsing", @"Services\LaunchPadProjectPageParseService.cs", 1228)] };

    var rootCause1 = new Exception("Upcoming");
    var fh1_Upcoming = new FaultHubException("Upcoming", rootCause1, ctxWhenUpcoming);

    var rootCause2 = new Exception("StartParsing");
    var fh2_StartParsing = new FaultHubException("StartParsing", rootCause2, ctxStartParsing);
    var agg2_1 = new AggregateException(fh2_StartParsing);
    var fh2_ParseTx = new FaultHubException("Project Parse Transaction failed", agg2_1, ctxParseTx);
    var agg2_2 = new AggregateException(fh2_ParseTx);
    var fh2_WhenExisting = new FaultHubException("When Existing Project Page Parsed failed", agg2_2, ctxWhenExisting);
    var agg2_3 = new AggregateException(fh2_WhenExisting);
    var fh2_ParseProjects = new FaultHubException("Parse Upcoming Projects failed", agg2_3, ctxParseProjects);
    
    var agg_mid = new AggregateException(fh1_Upcoming, fh2_ParseProjects);
    var fh_ParseUpcoming = new FaultHubException("Parse Up Coming failed", agg_mid, ctxParseUpcoming);
    var agg_top = new AggregateException(fh_ParseUpcoming);
    var topLevelException = new FaultHubException("Schedule Launch Pad Parse failed", agg_top, ctxSchedule);
            #endregion
            #endregion

            var reportString = topLevelException.Parse().Render();
            
            Console.WriteLine(reportString);

            #region ASSERT

            reportString.ShouldStartWith("Schedule Launch Pad Parse failed");
            reportString.ShouldContain("--- Failures (2) ---");

            reportString.ShouldContain("1. Failure Path:");
            reportString.ShouldContain("  - Operation: Schedule Launch Pad Parse (Kommunitas Kommunitas)");
            reportString.ShouldContain("    - Operation: Parse Up Coming (LaunchPad kommunitas)");
            reportString.ShouldContain("      - Operation: When Upcoming Urls");
            reportString.ShouldContain("      Root Cause: System.Exception: Upcoming");
            reportString.ShouldMatch(@"\(\) at WhenUpcomingUrls in .*Services\\LaunchPadProjectPageParseService\.cs:line 582");
            reportString.ShouldContain("2. Failure Path:");
            reportString.ShouldContain("          - Operation: Project Parse Transaction");
            reportString.ShouldContain("            - Operation: Start Parsing (driver, padProject)");
            reportString.ShouldContain("            Root Cause: System.Exception: StartParsing");
            reportString.ShouldMatch(@"\(\) at StartParsing in .*Services\\LaunchPadProjectPageParseService\.cs:line 1228");
            #endregion
        }
    }
}