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
    public class AutomatedRecoveryTests : FaultHubExtensionTestBase {
        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> FireRecoverablePrimitiveError()
            => Observable.Throw<Unit>(new Exception("Suppressed Failure"))
                .ContinueOnFault(context: ["RecoverablePrimitiveOp"]);

        [Test]
        public async Task Triggers_Recovery_For_Error_Suppressed_By_ContinueOnFault() {
            var recoveryActionExecuted = false;
            var recoveryRule = new RecoveryRule(
                Name: "Suppressed Error Recovery",
                Predicate: node => node.Name == nameof(FireRecoverablePrimitiveError),
                RecoveryAction: (_, _) => {
                    recoveryActionExecuted = true;
                    return Observable.Return(Unit.Default);
                }
            );

            var listener = FaultHub.Bus.OfType<FaultHubException>()
                .TriggerRecovery([recoveryRule])
                .Take(1);
            var trigger = FireRecoverablePrimitiveError().IgnoreElements().Select(_ => default(string));

            var result = await listener.Merge(trigger).Capture();

            recoveryActionExecuted.ShouldBeTrue();
            result.Items.Single().ShouldBe("Suppressed Error Recovery");
            
        }
        [Test]
        public async Task Recovery_Action_Is_Triggered_When_Rule_Matches() {
            var recoveryActionExecuted = false;
            var recoveryRule = new RecoveryRule(
                Name: "GetData Timeout Recovery",
                Predicate: node => node.Name == "GetData" && node.GetRootCause() is TimeoutException,
                RecoveryAction: (_, _) => {
                    recoveryActionExecuted = true;
                    return Observable.Return(Unit.Default);
                }
            );

            var captureTask = FaultHub.Bus.OfType<FaultHubException>()
                .TriggerRecovery([recoveryRule])
                .Take(1)
                .Capture();

            var exception = new FaultHubException(
                "Data fetch failed", 
                new TimeoutException("Connection timed out"),
                new AmbientFaultContext { BoundaryName = "GetData" });
            
            exception.Publish();

            var result = await captureTask;

            result.Error.ShouldBeNull();
            recoveryActionExecuted.ShouldBeTrue("The recovery action was not executed.");
            
            result.Items.Count.ShouldBe(1);
            result.Items.Single().ShouldBe("GetData Timeout Recovery");
        }
        
        [Test]
        public async Task No_Action_Is_Triggered_When_No_Rule_Matches() {
            var recoveryActionExecuted = false;
            var recoveryRule = new RecoveryRule(
                Name: "Some Other Recovery",
                Predicate: node => node.Name == "NonExistentOperation",
                RecoveryAction: (_, _) => {
                    recoveryActionExecuted = true;
                    return Observable.Return(Unit.Default);
                }
            );

            var captureTask = FaultHub.Bus.OfType<FaultHubException>()
                .TriggerRecovery([recoveryRule])
                .Timeout(TimeSpan.FromMilliseconds(100))
                .Catch(Observable.Empty<string>())
                .Capture();

            var exception = new FaultHubException(
                "Generic failure", 
                new InvalidOperationException(),
                new AmbientFaultContext { BoundaryName = "GenericOperation" });
    
            exception.Publish();
    
            var result = await captureTask;

            recoveryActionExecuted.ShouldBeFalse();
            result.Items.ShouldBeEmpty();
            result.Error.ShouldBeNull();
        }
        
    }
}