using System;
using System.Linq;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.Security.ClientServer;
using DevExpress.ExpressApp.SystemModule;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DevExpress.ExpressApp.Xpo;
using Microsoft.AspNetCore.Http;
using TestApplication.Blazor.Server.Services;
using Xpand.Extensions.LinqExtensions;
using Xpand.XAF.Modules.Reactive.Services;

namespace TestApplication.Blazor.Server {
    public class ServerBlazorApplication : BlazorApplication {
        public ServerBlazorApplication(){
            // HttpContext context;
            // context.Response.StatusCode=
            // IUriHelper
            
            Modules.Add(new BlazorModule());
            this.AlwaysUpdateOnDatabaseVersionMismatch().Subscribe();
            CheckCompatibilityType = CheckCompatibilityType.DatabaseSchema;
            
        }
        protected override void OnSetupStarted() {
            base.OnSetupStarted();
            IConfiguration configuration = ServiceProvider.GetRequiredService<IConfiguration>();
            if(configuration.GetConnectionString("ConnectionString") != null) {
                ConnectionString = configuration.GetConnectionString("ConnectionString");
            }
            if(configuration.GetConnectionString("EasyTestConnectionString") != null) {
                ConnectionString = configuration.GetConnectionString("EasyTestConnectionString");
            }

            
        }
        protected override void CreateDefaultObjectSpaceProvider(CreateCustomObjectSpaceProviderEventArgs args) {
            IXpoDataStoreProvider dataStoreProvider = GetDataStoreProvider(args.ConnectionString, args.Connection);
            args.ObjectSpaceProviders.Add(new XPObjectSpaceProvider( dataStoreProvider, true));
            args.ObjectSpaceProviders.Add(new NonPersistentObjectSpaceProvider(TypesInfo, null));
        }
        private IXpoDataStoreProvider GetDataStoreProvider(string connectionString, System.Data.IDbConnection connection) {
            XpoDataStoreProviderAccessor accessor = ServiceProvider.GetRequiredService<XpoDataStoreProviderAccessor>();
            lock(accessor) {
                if(accessor.DataStoreProvider == null) {
                    accessor.DataStoreProvider = XPObjectSpaceProvider.GetDataStoreProvider(connectionString, connection, true);
                }
            }
            return accessor.DataStoreProvider;
        }
        private void ServerBlazorApplication_DatabaseVersionMismatch(object sender, DatabaseVersionMismatchEventArgs e) {
            e.Updater.Update();
            e.Handled = true;

        }
    }
}
