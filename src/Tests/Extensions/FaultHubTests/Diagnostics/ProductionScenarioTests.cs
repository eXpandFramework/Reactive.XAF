using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.Relay;
using Xpand.Extensions.Reactive.Relay.Transaction;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.StringExtensions;
using Xpand.Extensions.Tracing;

namespace Xpand.Extensions.Tests.FaultHubTests.Diagnostics {
    [TestFixture]
    public class ProductionScenarioTests : ProductionScenarioBaseTest {
        [SetUp]
        public override void Setup() {
            base.Setup();
            
        }

        [Test][Apartment(ApartmentState.STA)]
        public async Task Replicates_Production_Report_Issue() {
            
            await ScheduleWebScraping().PublishFaults().Capture();
            BusEvents.Count.ShouldBe(1);
            var abortedException = BusEvents.Single().ShouldBeOfType<TransactionAbortedException>();
            var finalReport = abortedException;

            var reportLines = finalReport.ToString().ToLines().ToArray();

            AssertFaultExceptionReport(finalReport.ToString());
            reportLines.ShouldNotContain(line => line.Contains("Sequential Transaction"));
        }

        
    }
}