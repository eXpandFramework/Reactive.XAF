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
            var rootCause1 = new Exception("Upcoming");
            var logicalStack1 = new List<LogicalStackFrame>
                { new("WhenUpcomingUrls", @"C:\...\Services\LaunchPadProjectPageParseService.cs", 582) };
            var context1C = new AmbientFaultContext {
                BoundaryName = "WhenUpcomingUrls", UserContext = ["When Upcoming Urls"],
                LogicalStackTrace = logicalStack1
            };
            var context1B = new AmbientFaultContext {
                BoundaryName = "ParseUpComing", UserContext = ["Parse Up Coming", "LaunchPad kommunitas"],
                InnerContext = context1C
            };
            var context1A = new AmbientFaultContext {
                BoundaryName = "ScheduleLaunchPadParse",
                UserContext = ["Schedule Launch Pad Parse", "Kommunitas Kommunitas"], InnerContext = context1B
            };
            var faultPath1 = new FaultHubException("Wrapper1", rootCause1, context1A);

            var rootCause2 = new Exception("StartParsing");
            var logicalStack2 = new List<LogicalStackFrame>
                { new("StartParsing", @"C:\...\Services\LaunchPadProjectPageParseService.cs", 1228) };
            var context2F = new AmbientFaultContext {
                BoundaryName = "StartParsing", UserContext = ["Start Parsing", "driver", "padProject"],
                LogicalStackTrace = logicalStack2
            };
            var context2E = new AmbientFaultContext {
                BoundaryName = "ProjectParseTransaction", UserContext = ["Project Parse Transaction"],
                InnerContext = context2F
            };
            var context2D = new AmbientFaultContext {
                BoundaryName = "WhenExistingProjectPageParsed", UserContext = ["When Existing Project Page Parsed"],
                InnerContext = context2E
            };
            var context2C = new AmbientFaultContext {
                BoundaryName = "ParseUpcomingProjects",
                UserContext = ["Parse Upcoming Projects", "LaunchPad kommunitas"], InnerContext = context2D
            };
            var context2B = new AmbientFaultContext {
                BoundaryName = "ParseUpComing", UserContext = ["Parse Up Coming", "LaunchPad kommunitas"],
                InnerContext = context2C
            };
            var context2A = new AmbientFaultContext {
                BoundaryName = "ScheduleLaunchPadParse",
                UserContext = ["Schedule Launch Pad Parse", "Kommunitas Kommunitas"], InnerContext = context2B
            };
            var faultPath2 = new FaultHubException("Wrapper2", rootCause2, context2A);

            var aggregate = new AggregateException(faultPath1, faultPath2);
            var topLevelException =
                new FaultHubException("Parse Launch Pad failed", aggregate, new AmbientFaultContext());

            var reportModel = topLevelException.Parse();
            var reportString = reportModel.Render();

            Console.WriteLine(reportString);

            #region ASSERT

            reportModel.ShouldNotBeNull();
            reportModel.TopLevelMessage.ShouldBe("Parse Launch Pad failed");
            reportModel.Paths.Count.ShouldBe(2);

            var parsedPath1 = reportModel.Paths[0];
            var steps1 = parsedPath1.Steps;
            steps1.Count.ShouldBe(3);
            steps1[0].Name.ShouldBe("Schedule Launch Pad Parse");
            steps1[0].ContextData.ShouldBe(["Kommunitas Kommunitas"]);
            steps1[1].Name.ShouldBe("Parse Up Coming");
            steps1[1].ContextData.ShouldBe(["LaunchPad kommunitas"]);
            steps1[2].Name.ShouldBe("When Upcoming Urls");
            steps1[2].ContextData.ShouldBeEmpty();

            var parsedPath2 = reportModel.Paths[1];
            var steps2 = parsedPath2.Steps;
            steps2.Count.ShouldBe(6);
            steps2[5].Name.ShouldBe("Start Parsing");
            steps2[5].ContextData.ShouldBe(["driver", "padProject"]);

            reportString.ShouldStartWith("Parse Launch Pad failed");
            reportString.ShouldContain("--- Failures (2) ---");

            reportString.ShouldContain("1. Failure Path:");
            reportString.ShouldContain("  - Operation: Schedule Launch Pad Parse (Kommunitas Kommunitas)");
            reportString.ShouldContain("    - Operation: Parse Up Coming (LaunchPad kommunitas)");
            reportString.ShouldContain("      - Operation: When Upcoming Urls");
            reportString.ShouldContain("      Root Cause: System.Exception: Upcoming");
            reportString.ShouldContain(
                @"() at WhenUpcomingUrls in C:\...\Services\LaunchPadProjectPageParseService.cs:line 582");

            reportString.ShouldContain("2. Failure Path:");
            reportString.ShouldContain("        - Operation: Start Parsing (driver, padProject)");
            reportString.ShouldContain("        Root Cause: System.Exception: StartParsing");

            #endregion
        }
    }
}