// --- Full File: C:\Work\Reactive.XAF\src\Tests\Extensions\FaultHubTests\FaultHubExtensionTest\ProductionScenarioTests.cs ---

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.StringExtensions;
using TransactionAbortedException = Xpand.Extensions.Reactive.ErrorHandling.FaultHub.TransactionAbortedException;

namespace Xpand.Extensions.Tests.FaultHubTests._4_Diagnostics {
    [TestFixture]
    public class ProductionScenarioTests : ProductionScenarioBaseTest {
        
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