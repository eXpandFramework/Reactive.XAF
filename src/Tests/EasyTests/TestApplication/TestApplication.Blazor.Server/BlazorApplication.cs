using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor;
using Microsoft.Extensions.DependencyInjection;
using DevExpress.ExpressApp.Xpo;
using Microsoft.Extensions.Configuration;
using TestApplication.Blazor.Server.Services;
using TestApplication.Module.Blazor;
using TestApplication.Module.Common;
using Xpand.Extensions.Blazor;
using Xpand.XAF.Modules.Reactive.Services;

namespace TestApplication.Blazor.Server {

    public class ServerBlazorApplication : BlazorApplication,ISharedBlazorApplication {
        public ServerBlazorApplication(){
            Modules.Add(new TestBlazorModule());
            this.AlwaysUpdateOnDatabaseVersionMismatch().Subscribe();
            CheckCompatibilityType = CheckCompatibilityType.DatabaseSchema;
            DatabaseUpdateMode = DatabaseUpdateMode.UpdateDatabaseAlways;
        }
        protected override void OnSetupStarted() {
            base.OnSetupStarted();
            this.ConfigureConnectionString();
            ConnectionString = ServiceProvider.GetRequiredService<IConfiguration>().GetConnectionString("ConnectionString");
        }

        protected override void CreateDefaultObjectSpaceProvider(CreateCustomObjectSpaceProviderEventArgs args) {
            var dataStoreProvider = GetDataStoreProvider(args.ConnectionString, args.Connection);
            args.ObjectSpaceProviders.Add(new XPObjectSpaceProvider(dataStoreProvider, true));
            args.ObjectSpaceProviders.Add(new NonPersistentObjectSpaceProvider(TypesInfo, null));
        }

        private IXpoDataStoreProvider GetDataStoreProvider(string connectionString, System.Data.IDbConnection connection) {
            var accessor = ServiceProvider.GetRequiredService<XpoDataStoreProviderAccessor>();
            lock(accessor) {
                accessor.DataStoreProvider ??= XPObjectSpaceProvider.GetDataStoreProvider(connectionString, connection, true);
            }
            return accessor.DataStoreProvider;
        }

        public bool UseNonSecuredObjectSpaceProvider { get; set; }
    }
}
