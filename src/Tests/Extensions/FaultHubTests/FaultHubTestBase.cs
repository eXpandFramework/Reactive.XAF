using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.AssemblyExtensions;
using Xpand.Extensions.Numeric;
using Xpand.Extensions.Reactive.ErrorHandling;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;
using Xpand.Extensions.StringExtensions;

namespace Xpand.Extensions.Tests.FaultHubTests{
    public class FaultHubTestBase {
        protected List<Exception> BusEvents;
        private IDisposable _busSubscription;
        public FaultHubTestBase() => FaultHub.Logging = true;

        protected class TestResource : IDisposable {
            public bool IsDisposed { get; private set; }
            public Action OnDispose { get; init; } = () => { };
            public void Dispose() {
                IsDisposed = true;
                OnDispose();
            }
        }

        [TearDown]
        public void TearDown() {
            _busSubscription?.Dispose();
        }
        
        protected static IEnumerable<TestCaseData> RetrySelectors() {
            yield return new TestCaseData(RetrySelector).SetName("Retry");
            yield return new TestCaseData(RetrySelectorWithBackoff).SetName("RetrySelector");
        }

        private static Func<IObservable<Unit>,IObservable<Unit>> RetrySelector=>source => source.Retry(3);
        private static Func<IObservable<Unit>,IObservable<Unit>> RetrySelectorWithBackoff=>source => source.RetryWithBackoff(3, strategy:_ => 50.Milliseconds());
        [SetUp]
        public void Setup(){
            FaultHub.Seen.Clear();  
            BusEvents = new List<Exception>();
            _busSubscription = FaultHub.Bus.Subscribe(BusEvents.Add);
        }

        protected void AssertFaultExceptionReport(FaultHubException exception, [CallerMemberName] string caller = "") 
            => AssertFaultExceptionReport(exception.ToString(),caller);

        protected void AssertFaultExceptionReport(string report,[CallerMemberName]string caller="") {
            Clipboard.SetText(report);
            var storeReportLines = GetType().Assembly.ReadManifestResources(caller).First().ToLines().ToArray();
            var reportLines = report.ToLines().ToArray();
            Console.WriteLine("storeReport:");
            Console.WriteLine(storeReportLines);
            for (int i = 0; i < storeReportLines.Length-1; i++) {
                storeReportLines[i].ShouldBe(reportLines[i],$"Line {i+1}");
            }
        }

    }
}