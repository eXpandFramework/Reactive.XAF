using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.AssemblyExtensions;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.StringExtensions;

namespace Xpand.Extensions.Tests.FaultHubTests {
    [TestFixture]
    public class FaultHubExtensionTests:FaultHubTestBase {

        #region Operation Simulation
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        [SuppressMessage("ReSharper", "UnusedParameter.Local")]
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
        public async Task Generates_Concise_Execution_report_is_correct_and_readable() {
            await ScheduleLaunchPadParse().PublishFaults().Capture();
            BusEvents.Count.ShouldBe(1);
            var finalReport = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            
            var reportLines = finalReport.ToString().ToLines().ToArray();
            
            var storeReportLines = GetType().Assembly.ReadManifestResources(nameof(Generates_Concise_Execution_report_is_correct_and_readable)).First().ToLines().ToArray();
            Console.WriteLine("storeReport:");
            Console.WriteLine(storeReportLines);
            for (int i = 0; i < storeReportLines.Length-1; i++) {
                storeReportLines[i].ShouldBe(reportLines[i],$"Line {i+1}");
            }
        }
        
        [Test][Apartment(ApartmentState.STA)]
        public void Traverses_Parses_And_Renders_Complex_Nested_Exception_Correctly() {
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

            var finalReport = topLevelException;
            var reportLines = finalReport.ToString().ToLines().ToArray();

            var storeReportLines = GetType().Assembly.ReadManifestResources(nameof(Traverses_Parses_And_Renders_Complex_Nested_Exception_Correctly)).First().ToLines().ToArray();
            for (int i = 0; i < storeReportLines.Length-1; i++) {
                var storeReportLine = storeReportLines[i];
                storeReportLine.ShouldBe(reportLines[i],$"Line {i+1}");
            }
        }
        
                [Test]
        public async Task ToString_Report_Aggregates_Context_From_Nested_ChainFaultContext_Boundaries() {
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

            report.ShouldContain("OutermostContext");
            report.ShouldContain("IntermediateContext");
            report.ShouldContain("InnermostContext");

            report.ShouldContain("Deep Failure");
        }

    }
}