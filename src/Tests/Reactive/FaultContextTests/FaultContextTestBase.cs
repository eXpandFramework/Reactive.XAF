using NUnit.Framework;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;
using Xpand.XAF.Modules.Reactive.Services.Actions;
using Xpand.XAF.Modules.Reactive.Tests.Common;

namespace Xpand.XAF.Modules.Reactive.Tests.FaultContextTests{
    public abstract class FaultContextTestBase : ReactiveCommonTest {
        protected FaultContextTestBase() {
            FaultHub.Logging = true;
            
        }
        [SetUp]
        public override void Setup() {
            base.Setup();
            // ActionsService.ControllerCtorState.Clear();
            // FaultHub.Seen.Clear();
            // FaultHub.Reset();
        }

        [TearDown]
        public override void Dispose() {
            base.Dispose();
            FaultHub.Reset();
            ActionsService.ControllerCtorState.Clear();
            FaultHub.Seen.Clear();
        }
    }
}