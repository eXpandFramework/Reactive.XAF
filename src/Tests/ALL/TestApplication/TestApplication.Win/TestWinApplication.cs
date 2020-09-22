using System;
using System.Configuration;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Win;
using DevExpress.ExpressApp.Xpo;
using Fasterflect;
using Xpand.XAF.Modules.Reactive.Services;

namespace TestApplication.Win{
    public class TestWinApplication:WinApplication{
        public TestWinApplication(){
            UseOldTemplates = false;
            Modules.Add(new WinModule());
            var module = Modules.FindModule(typeof(ModuleBase));
            module.SetFieldValue("name", "Base");
            CheckCompatibilityType=CheckCompatibilityType.DatabaseSchema;
            this.AlwaysUpdateOnDatabaseVersionMismatch().Subscribe();    
        }

        protected override string GetModulesVersionInfoFilePath(){
            return null;
        }

        protected override void CreateDefaultObjectSpaceProvider(CreateCustomObjectSpaceProviderEventArgs args){
            args.ObjectSpaceProvider=new XPObjectSpaceProvider(new ConnectionStringDataStoreProvider(args.ConnectionString),true);
            // args.ObjectSpaceProvider = new XPObjectSpaceProvider(new ConnectionStringDataStoreProvider(ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString), true);
        }
    }
}