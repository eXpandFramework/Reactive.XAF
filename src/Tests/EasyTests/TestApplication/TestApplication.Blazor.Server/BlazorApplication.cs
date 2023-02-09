using System;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.Security.ClientServer;
using Microsoft.Extensions.DependencyInjection;
using DevExpress.ExpressApp.Xpo;
using Fasterflect;
using Microsoft.Extensions.Configuration;
using TestApplication.Blazor.Server.Services;
using TestApplication.Module.Blazor;
using TestApplication.Module.Common;
using Xpand.XAF.Modules.Reactive.Services;

namespace TestApplication.Blazor.Server {

    public class ServerBlazorApplication : BlazorApplication {
        public ServerBlazorApplication(){
            // Modules.Add(new TestBlazorModule());
            this.AlwaysUpdateOnDatabaseVersionMismatch().Subscribe();
            CheckCompatibilityType = CheckCompatibilityType.DatabaseSchema;
            DatabaseUpdateMode = DatabaseUpdateMode.UpdateDatabaseAlways;
            this.WhenViewOnFrame(typeof(AuthenticationStandardLogonParameters))
                .Do(frame => 
	                { frame.View.CurrentObject.SetPropertyValue("UserName", "Admin"); })
                .Subscribe();
        }
        protected override void OnSetupStarted() {
            base.OnSetupStarted();
            this.ConfigureConnectionString();
            // ConnectionString = ServiceProvider.GetRequiredService<IConfiguration>().GetConnectionString("ConnectionString");
        }

        // protected override void CreateDefaultObjectSpaceProvider(CreateCustomObjectSpaceProviderEventArgs args) {
        //     var dataStoreProvider = GetDataStoreProvider(args.ConnectionString, args.Connection);
        //     args.ObjectSpaceProviders.Add(new SecuredObjectSpaceProvider((ISelectDataSecurityProvider)Security,dataStoreProvider,true));
        //     args.ObjectSpaceProviders.Add(new NonPersistentObjectSpaceProvider(TypesInfo, null));
        // }

        private IXpoDataStoreProvider GetDataStoreProvider(string connectionString, System.Data.IDbConnection connection) {
            var accessor = ServiceProvider.GetRequiredService<XpoDataStoreProviderAccessor>();
            lock(accessor) {
                accessor.DataStoreProvider ??= XPObjectSpaceProvider.GetDataStoreProvider(connectionString, connection, true);
            }
            return accessor.DataStoreProvider;
        }

        
    }
}
