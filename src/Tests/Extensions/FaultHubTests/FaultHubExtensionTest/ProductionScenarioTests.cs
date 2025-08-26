using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.StringExtensions;

namespace Xpand.Extensions.Tests.FaultHubTests.FaultHubExtensionTest {
    [TestFixture]
    public class ProductionScenarioTests : FaultHubTestBase {

        #region Test Entry Point & Assertions
        [Test][Apartment(ApartmentState.STA)]
        public async Task Replicates_Production_Report_Issue() {
            await ScheduleLaunchPadParse().PublishFaults().Capture();

            BusEvents.Count.ShouldBe(1);
            var abortedException = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            var finalReport = abortedException;
            Clipboard.SetText(finalReport.ToString());
            Console.WriteLine("--- GENERATED REPORT ---");
            Console.WriteLine(finalReport.ToString());

            var reportLines = finalReport.ToString().ToLines().ToArray();

            reportLines.ShouldNotContain(line => line.Contains("Sequential Transaction"));
            
            AssertFaultExceptionReport(finalReport.ToString());
            
        }
        #endregion

        #region Mock Production Methods (Refactored without WithName)

        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> ScheduleLaunchPadParse()
            => ParseLaunchPad().ChainFaultContext(["LaunchPadName"])
                .PushStackFrame();
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> ParseLaunchPad()
            => Observable.Return("serviceModule")
                .SelectMany(_ => Observable.Return("pageParsed")) 
                .SelectMany(_ =>
                    ConnectLaunchPad()
                        .BeginBatchTransaction()
                        .Add(ParseUpComing())
                        .SequentialTransaction(true)
                )
                .PushStackFrame();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> ConnectWallet() => Observable.Return(Unit.Default);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> ConnectLaunchPad()
            => Observable.Return("navigated")
                .BeginBatchTransaction()
                .Add(ConnectWallet())
                .SequentialFailFastTransaction(context:["LaunchPadName","UserName"])
                .PushStackFrame()
            ;
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> ParseUpComing()
            => Observable.Return(new object[1])
                .BeginWorkflow(context:["LaunchPadName","UserName"])
                .Then(_ => WhenUpcomingUrls())
                .Then(_ => ParseUpcomingProjects())
                .RunToEnd()
                .ToUnit()
                .PushStackFrame()
            ;
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<string> WebSiteUrls() => Observable.Throw<string>(new Exception("Upcoming"));

        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<string[]> ExistingUpcomingUrl(string[] _) => Observable.Return(Array.Empty<string>());

        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<string[]> WhenUpcomingUrls()
            => Observable.Return(new object[1])
                .BeginWorkflow()
                .Then(_ => WebSiteUrls())
                .Then(ExistingUpcomingUrl)
                .RunToEnd()
                .SelectMany()
                .PushStackFrame();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> ParseUpcomingProjects()
            => new[] { ParseUpComingProject() }
                .BeginBatchTransaction()
                .SequentialTransaction()
                .PushStackFrame();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> ParseUpComingProject()
            => Observable.Return("project")
                .SelectMany(_ => ParseUpComingProjectsPlural())
                .BufferUntilCompleted().SelectMany();
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> ParseUpComingProjectsPlural()
            => WhenExistingProjectPageParsed()
                .PushStackFrame();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> WhenExistingProjectPageParsed()
            => ProjectParseTransaction()
                .BeginBatchTransaction()
                .Add(NotifyingTransaction())
                .SequentialTransaction()
                .ToUnit()
                .PushStackFrame();
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> Validate() => Observable.Return(Unit.Default);
        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> StartParsing() => Observable.Throw<Unit>(new Exception("StartParsing"));
        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> ParseNetwork() => Observable.Return(Unit.Default);
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> ProjectParseTransaction()
            => Validate()
                .BeginBatchTransaction()
                .Add(StartParsing())
                .Add(ParseNetwork())
                .SequentialTransaction();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> NotifyNewRounds() => Observable.Return(Unit.Default);
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> NotifyingTransaction()
            => Observable.Return(Unit.Default)
                .BeginBatchTransaction()
                .Add(NotifyNewRounds())
                .SequentialRunToEndTransaction()
                .PushStackFrame();
        
        #endregion
    }
}