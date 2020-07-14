using System;
using System.Configuration;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Win;
using DevExpress.ExpressApp.Xpo;
using Xpand.XAF.Modules.Reactive.Services;

namespace TestApplication.Win{
    public class TestWinApplication:WinApplication{
        public TestWinApplication(){
            Modules.Add(new WinModule());
	        CheckCompatibilityType=CheckCompatibilityType.DatabaseSchema;
            this.AlwaysUpdateOnDatabaseVersionMismatch().Subscribe();
        }

        protected override string GetModulesVersionInfoFilePath(){
            return null;
        }

        protected override void CreateDefaultObjectSpaceProvider(CreateCustomObjectSpaceProviderEventArgs args){
            // args.ObjectSpaceProvider=new XPObjectSpaceProvider(new MemoryDataStoreProvider(),true);
            args.ObjectSpaceProvider = new XPObjectSpaceProvider(
                new ConnectionStringDataStoreProvider(ConfigurationManager.ConnectionStrings["EasyTestConnectionString"].ConnectionString), true);
        }
    }
}