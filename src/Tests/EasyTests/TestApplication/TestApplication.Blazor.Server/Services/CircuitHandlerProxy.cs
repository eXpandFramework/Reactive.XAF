using System.Threading;
using System.Threading.Tasks;
using DevExpress.ExpressApp.Blazor.Services;
using Microsoft.AspNetCore.Components.Server.Circuits;

namespace TestApplication.Blazor.Server.Services {
    internal class CircuitHandlerProxy : CircuitHandler {
        private readonly IScopedCircuitHandler _scopedCircuitHandler;
        public CircuitHandlerProxy(IScopedCircuitHandler scopedCircuitHandler) {
            this._scopedCircuitHandler = scopedCircuitHandler;
        }
        public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken) {
            return _scopedCircuitHandler.OnCircuitOpenedAsync(cancellationToken);
        }
        public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken) {
            return _scopedCircuitHandler.OnConnectionUpAsync(cancellationToken);
        }
        public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken) {
            return _scopedCircuitHandler.OnCircuitClosedAsync(cancellationToken);
        }
        public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken) {
            return _scopedCircuitHandler.OnConnectionDownAsync(cancellationToken);
        }
    }
}
