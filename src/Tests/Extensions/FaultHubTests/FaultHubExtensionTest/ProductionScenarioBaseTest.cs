using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.Extensions.Tests.FaultHubTests.FaultHubExtensionTest{
    public abstract class ProductionScenarioBaseTest:FaultHubTestBase {
        #region Mock Production Methods (Refactored without WithName)

        [MethodImpl(MethodImplOptions.NoInlining)]
        protected IObservable<Unit> ScheduleLaunchPadParse()
            => ParseLaunchPad().ChainFaultContext(["LaunchPadName"])
                .PushStackFrame()
            ;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> ParseLaunchPad()
            => Observable.Return("serviceModule")
                .SelectMany(_ => Observable.Return("pageParsed")) 
                .SelectMany(_ => ConnectLaunchPad()
                        .BeginWorkflow()
                        .Then(ParseUpComing())
                        .RunFailFast()
                        .ToUnit()
                )
                .PushStackFrame();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> ConnectWallet() => Observable.Return(Unit.Default);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> ConnectLaunchPad()
            => Observable.Return("navigated")
                .BeginWorkflow("ConnectLaunchPad", context:["LaunchPadName","UserName"])
                .Then(ConnectWallet())
                .RunFailFast()
                .ToUnit()
        ;
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> ParseUpComing()
            => Observable.Return(new object[1])
                .BeginWorkflow(context:["LaunchPadName","UserName"])
                .Then(_ => WhenUpcomingUrls())
                .Then(_ => ParseUpcomingProjects())
                .RunToEnd()
                .ToUnit()
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
                .SelectMany() ;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> ParseUpcomingProjects()
            => new[] { ParseUpComingProject() }
                .BeginWorkflow("ParseUpcomingProjects", TransactionMode.Sequential)
                .RunToEnd()
                .ToUnit() ;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> ParseUpComingProject()
            => Observable.Return("project")
                .SelectMany(_ => ParseUpComingProjectsPlural())
                .BufferUntilCompleted().SelectMany();
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> ParseUpComingProjectsPlural()
            => WhenExistingProjectPageParsed();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> WhenExistingProjectPageParsed()
            => ProjectParseTransaction()
                .BeginWorkflow("WhenExistingProjectPageParsed")
                .Then(NotifyingTransaction())
                .RunToEnd()
                .ToUnit() ;
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> Validate() => Observable.Return(Unit.Default);
        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> StartParsing() => Observable.Throw<Unit>(new Exception("StartParsing"));
        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> ParseNetwork() => Observable.Return(Unit.Default);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> ProjectParseTransaction()
            => Validate()
                .BeginWorkflow("ProjectParseTransaction")
                .Then(StartParsing())
                .Then(ParseNetwork())
                .RunToEnd()
                .ToUnit();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> NotifyNewRounds() => Observable.Return(Unit.Default);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> NotifyingTransaction()
            => Observable.Return(Unit.Default)
                .BeginWorkflow("NotifyingTransaction")
                .Then(NotifyNewRounds())
                .RunToEnd()
                .ToUnit() ;
        
        #endregion
    }
}