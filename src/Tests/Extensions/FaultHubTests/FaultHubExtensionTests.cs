using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;
using Xpand.TestsLib.Common.Win32;

namespace Xpand.Extensions.Tests.FaultHubTests {
    [TestFixture]
    public class FaultHubExtensionTests {

        [Test][Apartment(ApartmentState.STA)]
        public void Traverses_Parses_And_Renders_Complex_Nested_Exception_Correctly() {
            #region ARRANGE

            #region ARRANGE

            var ctxSchedule = new AmbientFaultContext {
                BoundaryName = "ScheduleLaunchPadParse()",
                UserContext = ["Schedule Launch Pad Parse", "Kommunitas Kommunitas"]
            };
            var ctxParseUpcoming = new AmbientFaultContext
                { BoundaryName = "ParseUpComing", UserContext = ["Parse Up Coming", "LaunchPad kommunitas"] };
            var ctxWhenUpcoming = new AmbientFaultContext {
                BoundaryName = "WhenUpcomingUrls(serviceModule, driver, launchPad)",
                UserContext = ["When Upcoming Urls"],
                LogicalStackTrace =
                    [new LogicalStackFrame("WhenUpcomingUrls", @"Services\LaunchPadProjectPageParseService.cs", 582)]
            };
            var ctxParseProjects = new AmbientFaultContext {
                BoundaryName = "ParseUpcomingProjects()",
                UserContext = ["Parse Upcoming Projects", "LaunchPad kommunitas"]
            };
            var ctxWhenExisting = new AmbientFaultContext
                { BoundaryName = "WhenExistingProjectPageParsed", UserContext = ["When Existing Project Page Parsed"] };
            var ctxParseTx = new AmbientFaultContext
                { BoundaryName = "ProjectParseTransaction()", UserContext = ["Project Parse Transaction"] };
            var ctxStartParsing = new AmbientFaultContext {
                BoundaryName = "StartParsing(driver, padProject)",
                UserContext = ["Start Parsing", "driver", "padProject"],
                LogicalStackTrace =
                    [new LogicalStackFrame("StartParsing", @"Services\LaunchPadProjectPageParseService.cs", 1228, "MyDynamicValue")]
            };

            var rootCause1 = new Exception("Upcoming");
            var fh1Upcoming = new FaultHubException("Upcoming", rootCause1, ctxWhenUpcoming);

            var rootCause2 = new Exception("StartParsing");
            var fh2StartParsing = new FaultHubException("StartParsing", rootCause2, ctxStartParsing);
            var agg21 = new AggregateException(fh2StartParsing);
            var fh2ParseTx = new FaultHubException("Project Parse Transaction failed", agg21, ctxParseTx);
            var agg22 = new AggregateException(fh2ParseTx);
            var fh2WhenExisting =
                new FaultHubException("When Existing Project Page Parsed failed", agg22, ctxWhenExisting);
            var agg23 = new AggregateException(fh2WhenExisting);
            var fh2ParseProjects = new FaultHubException("Parse Upcoming Projects failed", agg23, ctxParseProjects);

            var aggMid = new AggregateException(fh1Upcoming, fh2ParseProjects);
            var fhParseUpcoming = new FaultHubException("Parse Up Coming failed", aggMid, ctxParseUpcoming);
            var aggTop = new AggregateException(fhParseUpcoming);
            var topLevelException = new FaultHubException("Schedule Launch Pad Parse failed", aggTop, ctxSchedule);

            #endregion

            #endregion

            var reportString = topLevelException.Parse().Render();

            Console.WriteLine(reportString);
            Clipboard.SetText(reportString);
            #region ASSERT

            #region ASSERT
            reportString.ShouldStartWith("Schedule Launch Pad Parse failed (2 times)");
            reportString.ShouldNotContain("--- Failures");
            reportString.ShouldNotContain("Failure Path:");
            reportString.ShouldNotContain("Operation:");

            // Common Path
            reportString.ShouldContain("  Schedule Launch Pad Parse (Kommunitas Kommunitas)");
            reportString.ShouldContain("    Parse Up Coming (LaunchPad kommunitas)");
    
            // Divergent paths should be numbered, but the top-level common path should not.
            reportString.ShouldNotContain("1. Schedule Launch Pad Parse");
            reportString.ShouldNotContain("2. Schedule Launch Pad Parse");

            // Divergent Path 1
            reportString.ShouldMatch(@"\s+1\. When Upcoming Urls");
            reportString.ShouldMatch(@"\s+• Root Cause: System\.Exception: Upcoming");
            reportString.ShouldMatch(@"\s+--- Invocation Stack ---\s+at WhenUpcomingUrls in LaunchPadProjectPageParseService\.cs:line 582");
            reportString.ShouldNotMatch(@"\s+\(.*\)\s+at WhenUpcomingUrls");

            // Divergent Path 2
            reportString.ShouldMatch(@"\s+2\. Parse Upcoming Projects \(LaunchPad kommunitas\)");
            reportString.ShouldMatch(@"\s+When Existing Project Page Parsed");
            reportString.ShouldMatch(@"\s+Project Parse Transaction");
            reportString.ShouldMatch(@"\s+Start Parsing");
            reportString.ShouldMatch(@"\s+• Root Cause: System\.Exception: StartParsing");
            reportString.ShouldMatch(@"\s+--- Invocation Stack ---\s+\(MyDynamicValue\) at StartParsing in LaunchPadProjectPageParseService\.cs:line 1228");
            #endregion

            #endregion
        }
    }
}