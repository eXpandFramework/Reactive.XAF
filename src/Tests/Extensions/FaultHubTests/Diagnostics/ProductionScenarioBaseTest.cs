using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.Extensions.Tests.FaultHubTests.Diagnostics{
    public abstract class ProductionScenarioBaseTest:FaultHubTestBase {
        #region Mock Production Methods (Refactored)

        [MethodImpl(MethodImplOptions.NoInlining)]
        protected IObservable<Unit> ScheduleWebScraping()
            => ParseHomePage()
                .PushStackFrame(["Scheduled Context"])
                .ChainFaultContext(["HomePageUrl"]);
        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> ParseHomePage()
            => Observable.Return("serviceModule")
                .SelectMany(_ => Observable.Return("pageParsed")
                        .BeginWorkflow()
                        .Then(NavigateToHomePage())
           
                     .Then(ExtractAndProcessLinks())
                        .RunFailFast()
                        .ToUnit()
                )
                .PushStackFrame(["HomePageUrl"]);
        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> AcceptCookies() => Observable.Return(Unit.Default);
        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> NavigateToHomePage()
            => Observable.Return("navigated")
                .BeginWorkflow("NavigateToHomePage", context:["HomePageUrl","UserAgent"])
                .Then(AcceptCookies())
                .RunFailFast()
                .ToUnit()
        ;
        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> ExtractAndProcessLinks()
            => Observable.Return(new object[1])
                .BeginWorkflow(context:["HomePageUrl","UserAgent"])
                .Then(_ => GetPageLinks())
                .Then(_ => ScrapeDataFromLinks())
                .RunToEnd()
        
                 .ToUnit()
        ;
        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<string> FetchInitialUrls() => Observable.Timer(TimeSpan.FromMilliseconds(240)).SelectMany(_ => Observable.Throw<string>(new Exception("Failed to fetch URLs")));
        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<string[]> FilterExistingUrls(string[] _) => Observable.Return(Array.Empty<string>());
        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<string[]> GetPageLinks()
            => Observable.Return(new object[1])
                .BeginWorkflow()
                .Then(_ => FetchInitialUrls())
                .Then(FilterExistingUrls)
                .RunToEnd()
          
               .SelectMany() ;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> ScrapeDataFromLinks()
            => new[] { ScrapeDataFromLink() }
                .BeginWorkflow("ScrapeDataFromLinks", TransactionMode.Sequential)
                .RunToEnd()
                .ToUnit() ;
        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> ScrapeDataFromLink()
            => Observable.Return("content")
                .SelectMany(_ => ScrapeAllLinks())
                .BufferUntilCompleted().SelectMany();
        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> ScrapeAllLinks()
            => WhenLinkScraped();
        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> WhenLinkScraped()
            => DataExtractionTransaction()
                .BeginWorkflow("WhenLinkScraped")
                .Then(NotificationTransaction())
                .RunToEnd()
                .ToUnit() ;
        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> ValidatePage() => Observable.Return(Unit.Default);
        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> ExtractContent() => Observable.Timer(TimeSpan.FromMilliseconds(200)).SelectMany(_ => Observable.Throw<Unit>(new Exception("Failed to extract content")));
        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> ParseMetadata() => Observable.Return(Unit.Default);
        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> DataExtractionTransaction()
            => ValidatePage()
                .BeginWorkflow("DataExtractionTransaction")
                .Then(ExtractContent())
                .Then(ParseMetadata())
                .RunToEnd()
             
                   .ToUnit();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> NotifyNewData() => Observable.Return(Unit.Default);
        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> NotificationTransaction()
            => Observable.Return(Unit.Default)
                .BeginWorkflow("NotificationTransaction")
                .Then(NotifyNewData())
                .RunToEnd()
                .ToUnit() ;
        #endregion
    }
}