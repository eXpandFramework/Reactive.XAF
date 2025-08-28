using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.AssemblyExtensions;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Numeric;
using Xpand.Extensions.ObjectExtensions;
using Xpand.Extensions.Reactive.ErrorHandling;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;
using Xpand.Extensions.StringExtensions;

namespace Xpand.Extensions.Tests.FaultHubTests{
    public class FaultHubTestBase {
        protected List<Exception> BusEvents;
        private IDisposable _busSubscription;
        private Dictionary<string, string> _regexes;
        public FaultHubTestBase() {
            FaultHub.Logging = true;
            
        }

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
            FaultHub.BlacklistedFilePathRegexes.Clear();
        }
        
        protected static IEnumerable<TestCaseData> RetrySelectors() {
            yield return new TestCaseData(RetrySelector).SetName("Retry");
            yield return new TestCaseData(RetrySelectorWithBackoff).SetName("RetrySelector");
        }

        private static Func<IObservable<Unit>,IObservable<Unit>> RetrySelector=>source => source.Retry(3);
        private static Func<IObservable<Unit>,IObservable<Unit>> RetrySelectorWithBackoff=>source => source.RetryWithBackoff(3, strategy:_ => 50.Milliseconds());
        [SetUp]
        public void Setup(){
            
            // FaultHub.BlacklistedFilePathRegexes.Add("Reactive\\.XAF\\\\src\\\\Extensions", "Test regex");
            FaultHub.BlacklistedFilePathRegexes.Clear();
            FaultHub.Seen.Clear();  
            BusEvents = new List<Exception>();
            _busSubscription = FaultHub.Bus.Subscribe(BusEvents.Add);
        }

        protected void AssertFaultExceptionReport(FaultHubException exception, [CallerMemberName] string caller = "") 
            => AssertFaultExceptionReport(exception.ToString(),caller);

        protected void AssertFaultExceptionReport(string report,[CallerMemberName]string caller="") {
            Console.WriteLine("--- GENERATED REPORT ---");
            Console.WriteLine(report);
            Clipboard.SetText(report);
            var reportLines = GetType().Assembly.ReadManifestResources(caller).First().ToLines().ToArray();
            var storeReportLines = report.ToLines().ToArray();
            Console.WriteLine("storeReport:");
            Console.WriteLine(storeReportLines);
            for (int i = 0; i < storeReportLines.Length-1; i++) {
                var storeLine = storeReportLines[i];
                var reportLine = reportLines[i];

                var regex = new Regex(@" in (.*?):line (\d+)");

                var storeMatch = regex.Match(storeLine);
                var reportMatch = regex.Match(reportLine);

                var finalStoreLine = storeLine;
                var finalReportLine = reportLine;

                if (storeMatch.Success) {
                    var fullPath = storeMatch.Groups[1].Value;
                    if (fullPath.Contains(Path.DirectorySeparatorChar)) {
                        var fileName = Path.GetFileName(fullPath);
                        finalStoreLine = storeLine.Replace(fullPath, fileName);
                    }
                }

                if (reportMatch.Success) {
                    var fullPath = reportMatch.Groups[1].Value;
                    if (fullPath.Contains(Path.DirectorySeparatorChar)) {
                        var fileName = Path.GetFileName(fullPath);
                        finalReportLine = reportLine.Replace(fullPath, fileName);
                    }
                }

                finalStoreLine.ShouldBe(finalReportLine, $"Line {i + 1}");
            }
        }

    }
}