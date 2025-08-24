using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Unit = System.Reactive.Unit;

namespace Xpand.Extensions.Tests.FaultHubTests {
    [TestFixture]
    public class FaultHubExceptionToStringTests : FaultHubTestBase {
        public static FaultHubException LastReport { get; private set; }
        #region Operation Simulation
        private IObservable<Unit> StartParsing()
            => Observable.Throw<Unit>(new Exception("StartParsing"))
                .PushStackFrame();

        private IObservable<Unit> ProjectParseTransaction()
            => Unit.Default.Observe().BeginWorkflow("Project Parse Transaction")
                .Then(_ => StartParsing(), "Start Parsing")
                .RunToEnd().ToUnit();

        private IObservable<Unit> WhenExistingProjectPageParsed()
            => Unit.Default.Observe().BeginWorkflow("When Existing Project Page Parsed")
                .Then(_ => ProjectParseTransaction())
                .RunToEnd().ToUnit() ;

        private IObservable<Unit> ParseUpcomingProjects()
            => Unit.Default.Observe().BeginWorkflow("Parse Upcoming Projects")
                .Then(_ => WhenExistingProjectPageParsed())
                .RunToEnd().ToUnit()
                .PushStackFrame();

        private IObservable<Unit> WhenUpcomingUrls()
            => Observable.Throw<Unit>(new Exception("Upcoming"))
                .PushStackFrame();

        private IObservable<Unit> ParseUpComing()
            => Unit.Default.Observe().BeginWorkflow("Parse Up Coming")
                .Then(_ => WhenUpcomingUrls())
                .Then(_ => ParseUpcomingProjects())
                .RunToEnd().ToUnit();
        
        private IObservable<Unit> ScheduleLaunchPadParse()
            => Unit.Default.Observe().BeginWorkflow("Schedule Launch Pad Parse")
                .Then(_ => ParseUpComing())
                .RunToEnd().ToUnit();
        #endregion

        
        [Test][Apartment(ApartmentState.STA)]
        public async Task Generates_Concise_Execution_Report_For_Complex_Nested_Failures() {
            await ScheduleLaunchPadParse().PublishFaults().Capture();

            BusEvents.Count.ShouldBe(1);
            var finalReport = BusEvents.Single().ShouldBeOfType<FaultHubException>();
    
            LastReport = finalReport;
            var reportString = finalReport.ToString();
            Console.WriteLine(reportString);
            Clipboard.SetText(reportString);

            reportString.ShouldStartWith("Schedule Launch Pad Parse completed with errors");
            reportString.ShouldContain("--- Failures (2) ---");

            reportString.ShouldContain("1. Failure Path:");
            reportString.ShouldMatch(@"-\s+Operation: Schedule Launch Pad Parse");
            reportString.ShouldMatch(@"\s+-\s+Operation: Parse Up Coming");
            reportString.ShouldMatch(@"\s+-\s+Operation: When Upcoming Urls");
            reportString.ShouldContain("Root Cause: System.Exception: Upcoming");
            reportString.ShouldMatch(@"\(\) at WhenUpcomingUrls in .*Tests\\Extensions\\FaultHubTests\\FaultHubExceptionToStringTests.cs:line \d+");
    
            reportString.ShouldContain("2. Failure Path:");
            reportString.ShouldMatch(@"-\s+Operation: Schedule Launch Pad Parse");
            reportString.ShouldMatch(@"\s+-\s+Operation: Parse Up Coming");
            reportString.ShouldMatch(@"\s+-\s+Operation: Parse Upcoming Projects");
            reportString.ShouldMatch(@"\s+-\s+Operation: When Existing Project Page Parsed");
            reportString.ShouldMatch(@"\s+-\s+Operation: Project Parse Transaction");
            reportString.ShouldMatch(@"\s+-\s+Operation: Start Parsing");
            reportString.ShouldContain("Root Cause: System.Exception: StartParsing");
            reportString.ShouldMatch(@"\(\) at StartParsing in .*Tests\\Extensions\\FaultHubTests\\FaultHubExceptionToStringTests.cs:line \d+");

            reportString.ShouldNotContain("as part of:");
            // reportString.ShouldNotContain("C:\\");
            
        }
    }
}