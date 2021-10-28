using System.Threading.Tasks;
using DevExpress.ExpressApp.Blazor.Services;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TestApplication.Blazor.Server.Services {
    internal class ProxyHubConnectionHandler<THub> : HubConnectionHandler<THub> where THub : Hub {
        private readonly IValueManagerStorageContainerInitializer _storageContainerInitializer;
        public ProxyHubConnectionHandler(
            HubLifetimeManager<THub> lifetimeManager,
            IHubProtocolResolver protocolResolver,
            IOptions<HubOptions> globalHubOptions,
            IOptions<HubOptions<THub>> hubOptions,
            ILoggerFactory loggerFactory,
            IUserIdProvider userIdProvider,
            IServiceScopeFactory serviceScopeFactory,
            IValueManagerStorageContainerInitializer storageContainerAccessor)
            : base(lifetimeManager, protocolResolver, globalHubOptions, hubOptions, loggerFactory, userIdProvider, serviceScopeFactory) {
            this._storageContainerInitializer = storageContainerAccessor;
        }

        public override Task OnConnectedAsync(ConnectionContext connection) {
            _storageContainerInitializer.Initialize();
            return base.OnConnectedAsync(connection);
        }
    }
}
