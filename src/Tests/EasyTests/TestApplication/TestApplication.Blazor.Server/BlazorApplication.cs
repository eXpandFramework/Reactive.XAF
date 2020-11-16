using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DevExpress.ExpressApp.Xpo;
using TestApplication.Blazor.Server.Services;
using Xpand.XAF.Modules.Reactive.Services;

namespace TestApplication.Blazor.Server {
    public class ServerBlazorApplication : BlazorApplication {
        public ServerBlazorApplication(){
            Modules.Add(new BlazorModule());
            this.AlwaysUpdateOnDatabaseVersionMismatch().Subscribe();
            CheckCompatibilityType = CheckCompatibilityType.DatabaseSchema;
            
        }
        protected override void OnSetupStarted() {
            base.OnSetupStarted();
            this.ConfigureConnectionString();
            // ConnectionString = ServiceProvider.GetRequiredService<IConfiguration>().GetConnectionString("ConnectionString");

        }
        protected override void CreateDefaultObjectSpaceProvider(CreateCustomObjectSpaceProviderEventArgs args) {
            IXpoDataStoreProvider dataStoreProvider = GetDataStoreProvider(args.ConnectionString, args.Connection);
            args.ObjectSpaceProviders.Add(new XPObjectSpaceProvider( dataStoreProvider, true));
            args.ObjectSpaceProviders.Add(new NonPersistentObjectSpaceProvider(TypesInfo, null));
        }
        private IXpoDataStoreProvider GetDataStoreProvider(string connectionString, System.Data.IDbConnection connection) {
            XpoDataStoreProviderAccessor accessor = ServiceProvider.GetRequiredService<XpoDataStoreProviderAccessor>();
            lock(accessor) {
                accessor.DataStoreProvider ??= XPObjectSpaceProvider.GetDataStoreProvider(connectionString, connection, true);
            }
            return accessor.DataStoreProvider;
        }
    }
}
