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
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private IObservable<Unit> StepWithArgs(string someNoisyArgument) => WhenExistingProjectPageParsed()
            .PushStackFrame();

        private IObservable<Unit> StartParsing()
            => Observable.Throw<Unit>(new Exception("StartParsing"))
                .PushStackFrame();

        private IObservable<Unit> ProjectParseTransaction()
            => Unit.Default.Observe().BeginWorkflow("Project Parse Transaction")
                .Then(_ => StartParsing(), "Start Parsing")
                .RunToEnd().ToUnit()
                .PushStackFrame();

        private IObservable<Unit> WhenExistingProjectPageParsed()
            => Unit.Default.Observe().BeginWorkflow("When Existing Project Page Parsed")
                .Then(_ => ProjectParseTransaction())
                .RunToEnd().ToUnit()
                .PushStackFrame();

        private IObservable<Unit> ParseUpcomingProjects()
            => Unit.Default.Observe().BeginWorkflow("Parse Upcoming Projects")
                .Then(_ => StepWithArgs("some noisy argument"))
                .RunToEnd().ToUnit()
                .PushStackFrame();
        
        private IObservable<Unit> WhenUpcomingUrls()
            => Observable.Throw<Unit>(new Exception("Upcoming"))
                .PushStackFrame();

        private IObservable<Unit> ParseUpComing()
            => Unit.Default.Observe().BeginWorkflow("Parse Up Coming")
                .Then(_ => WhenUpcomingUrls())
                .Then(_ => ParseUpcomingProjects())
                .RunToEnd().ToUnit()
                .PushStackFrame();
        
        private IObservable<Unit> ScheduleLaunchPadParse()
            => Unit.Default.Observe().BeginWorkflow("Schedule Launch Pad Parse")
                .Then(_ => ParseUpComing())
                .RunToEnd().ToUnit()
                .PushStackFrame();
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

            reportString.ShouldContain("1. Schedule Launch Pad Parse");
            reportString.ShouldContain("   Parse Up Coming");
            reportString.ShouldContain("     When Upcoming Urls");
            reportString.ShouldContain("   • Root Cause: System.Exception: Upcoming");
            reportString.ShouldMatch(@"\s+--- Invocation Stack ---\s+at WhenUpcomingUrls in FaultHubExceptionToStringTests.cs:line \d+");
            reportString.ShouldNotMatch(@"\s+\(\) at");

            // -- Verify Path 2 --
            reportString.ShouldContain("2. Schedule Launch Pad Parse");
            reportString.ShouldContain("   Parse Up Coming");
            reportString.ShouldContain("     Parse Upcoming Projects");
            reportString.ShouldContain("       Step With Args");
            reportString.ShouldContain("         When Existing Project Page Parsed");
            reportString.ShouldContain("           Project Parse Transaction");
            reportString.ShouldContain("             Start Parsing");
            reportString.ShouldContain("   • Root Cause: System.Exception: StartParsing");
            reportString.ShouldMatch(@"\s+--- Invocation Stack ---\s+at StartParsing in FaultHubExceptionToStringTests.cs:line \d+");
        }
    }
}