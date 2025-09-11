using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Tests.FaultHubTests.Diagnostics.FaultHubQueryingTests{
    [TestFixture]
    public class AlertingServiceTests : FaultHubExtensionTestBase {
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> FirePrimitiveOperationError()
            => Observable.Throw<Unit>(new Exception("Primitive Failure"))
                .ChainFaultContext(["PrimitiveOperation"]);

        [Test]
        public async Task Triggers_Alert_From_Primitive_ChainFaultContext_Operator() {
            var alertRule = new AlertRule(
                Name: "Primitive Operation Failure",
                Severity: AlertSeverity.Error,
                Predicate: node => node.Name == nameof(FirePrimitiveOperationError)
            );

            var listener = FaultHub.Bus.ToAlert(alertRule).Take(1);
            var trigger = FirePrimitiveOperationError().PublishFaults().IgnoreElements().Select(_ => default(Alert));

            var result = await listener.Merge(trigger).Capture();

            result.Items.Count.ShouldBe(1);
            var alert = result.Items.Single();
            alert.RuleName.ShouldBe("Primitive Operation Failure");
            alert.Exception.Context.BoundaryName.ShouldBe(nameof(FirePrimitiveOperationError));
        }
        [Test]
        public async Task Dispatches_Alert_When_Rule_Matches() {
            var criticalPaymentRule = new AlertRule(
                Name: "Critical Payment Transaction Failure",
                Severity: AlertSeverity.Critical,
                Predicate: node => node.Name == "ProcessPayment" && node.Tags.Contains(Transaction.TransactionNodeTag),
                MessageSelector: rootNode => {
                    var customerId = rootNode.Descendants()
                        .SelectMany(n => n.ContextData)
                        .FirstOrDefault(d => d.ToString()!.StartsWith("CustomerID")) ?? "Unknown";
                    return $"Payment transaction failed for CustomerID: {customerId}";
                }
            );

            var captureTask = FaultHub.Bus.ToAlert(criticalPaymentRule).Take(1).Capture();

            var paymentException = new TransactionAbortedException(
                "Transaction failed",
                CreateNestedFault(("ValidateCustomer", ["CustomerID: 456"])),
                new AmbientFaultContext {
                    BoundaryName = "ProcessPayment",
                    Tags = [Transaction.TransactionNodeTag]
                });

            paymentException.Publish();
    
            var result = await captureTask;

            result.Error.ShouldBeNull();
            result.Items.Count.ShouldBe(1);

            var alert = result.Items.Single();
            alert.RuleName.ShouldBe("Critical Payment Transaction Failure");
            alert.Severity.ShouldBe(AlertSeverity.Critical);
            alert.Message.ShouldBe("Payment transaction failed for CustomerID: CustomerID: 456");
            alert.Exception.ShouldBe(paymentException);
        }
        
        [Test]
        public async Task No_Action_Is_Triggered_When_No_Rule_Matches() {
            var recoveryRule = new AlertRule(
                Name: "Some Other Rule",
                Severity: AlertSeverity.Warning,
                Predicate: node => node.Name == "NonExistentOperation"
            );

            var captureTask = FaultHub.Bus.ToAlert(recoveryRule).Take(1)
                .Timeout(TimeSpan.FromMilliseconds(100))
                .Catch(Observable.Empty<Alert>())
                .Capture();

            var exception = new FaultHubException(
                "Generic failure", 
                new InvalidOperationException(),
                new AmbientFaultContext { BoundaryName = "GenericOperation" });
            
            exception.Publish();
            
            var result = await captureTask;

            result.Items.ShouldBeEmpty();
            result.Error.ShouldBeNull();
        }
    
    }
}