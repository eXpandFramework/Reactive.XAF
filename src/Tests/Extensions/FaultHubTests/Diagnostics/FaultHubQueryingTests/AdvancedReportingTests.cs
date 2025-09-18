using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.FaultHub;
using Xpand.Extensions.Reactive.FaultHub.Transaction;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Tests.FaultHubTests.Diagnostics.FaultHubQueryingTests {
    [TestFixture]
    public class AdvancedReportingTests : FaultHubExtensionTestBase {
        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> FireMetricsPrimitiveError()
            => Observable.Throw<Unit>(new InvalidOperationException("Primitive Metric Failure"))
                .ChainFaultContext(["MetricsPrimitiveOp"]);

        [Test]
        public async Task ToFailureMetrics_Correctly_Extracts_Data_From_Primitive_ChainFaultContext() {
            var listener = FaultHub.Bus.OfType<FaultHubException>().ToFailureMetrics().Take(1);
            var trigger = FireMetricsPrimitiveError().PublishFaults().IgnoreElements().Select(_ => default(FailureMetric));

            var result = await listener.Merge(trigger).Capture();

            result.Items.Count.ShouldBe(1);
            var metric = result.Items.Single();

            metric.TransactionName.ShouldBe(nameof(FireMetricsPrimitiveError));
            metric.StepName.ShouldBe(nameof(FireMetricsPrimitiveError));
            metric.RootCauseType.ShouldBe(typeof(InvalidOperationException).FullName);
            
        }
        [Test]
        public async Task ToFailureMetrics_Correctly_Extracts_Data_From_Complex_Exception() {
            var transaction = Observable.Return("start")
                .BeginWorkflow("MyTestTransaction")
                .Then(_ => Observable.Throw<string>(new TimeoutException()), "MyFailingStep")
                .RunToEnd();

            var captureTask = FaultHub.Bus.ToFailureMetrics().Take(1).Capture();

            await transaction.PublishFaults().Capture();

            var result = await captureTask;

            result.Error.ShouldBeNull();
            result.Items.Count.ShouldBe(1);
            var metric = result.Items.Single();

            metric.TransactionName.ShouldBe("MyTestTransaction");
            metric.StepName.ShouldBe("MyFailingStep");
            metric.RootCauseType.ShouldBe(typeof(TimeoutException).FullName);
            metric.Tags.ShouldContain(Transaction.StepNodeTag);
        }

        
        [Test]
        public async Task Can_Group_And_Count_Metrics_By_Transaction_Name() {
            var exceptionA1 = new FaultHubException("Error in Tx A", new InvalidOperationException(),
                new AmbientFaultContext { BoundaryName = "TransactionA", Tags = [Transaction.TransactionNodeTag] });
    
            var exceptionA2 = new FaultHubException("Another error in Tx A", new TimeoutException(),
                new AmbientFaultContext { BoundaryName = "TransactionA", Tags = [Transaction.TransactionNodeTag] });

            var exceptionB = new FaultHubException("Error in Tx B", new InvalidOperationException(),
                new AmbientFaultContext { BoundaryName = "TransactionB", Tags = [Transaction.TransactionNodeTag] });

            var exceptions = new[] { exceptionA1, exceptionA2, exceptionB };

            var results = await exceptions.ToObservable(TaskPoolScheduler.Default)
                .ToFailureMetrics()
                .GroupBy(metric => metric.TransactionName)
                .SelectMany(group => group.Count().Select(count => new { Name = group.Key, Count = count }))
                .ToList();

            results.Count.ShouldBe(2);
            results.ShouldContain(r => r.Name == "TransactionA" && r.Count == 2);
            results.ShouldContain(r => r.Name == "TransactionB" && r.Count == 1);
        }
    }
}