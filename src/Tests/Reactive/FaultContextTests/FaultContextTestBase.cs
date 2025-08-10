using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;
using Xpand.XAF.Modules.Reactive.Tests.Common;

namespace Xpand.XAF.Modules.Reactive.Tests.FaultContextTests{
    public abstract class FaultContextTestBase : ReactiveCommonTest {
        protected FaultContextTestBase() {
            FaultHub.Logging = true;
        }
    }
}