using System;
using System.Configuration;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.Security.ClientServer;
using DevExpress.ExpressApp.Win;
using DevExpress.ExpressApp.Xpo;
using Xpand.XAF.Modules.Reactive.Services;

namespace TestApplication.Win{
    public class TestWinApplication:WinApplication{
        public TestWinApplication(){
	        DevExpress.ExpressApp.Utils.ImageLoader.Instance.UseSvgImages = true;
            UseOldTemplates = false;
            LinkNewObjectToParentImmediately = false;
            OptimizedControllersCreation = true;
            UseLightStyle = true;
            ExecuteStartupLogicBeforeClosingLogonWindow = true;
            Modules.Add(new TestApplication.Module.Win.TestApplicationWinModule());
            CheckCompatibilityType=CheckCompatibilityType.DatabaseSchema;
            this.AlwaysUpdateOnDatabaseVersionMismatch().Subscribe();
            ModelCacheManager.UseCacheWhenDebuggerIsAttached = true;
        }

        protected override string GetModelAssemblyFilePath(){
            return null;
        }


        protected override string GetModulesVersionInfoFilePath(){
            return null;
        }

        protected override void CreateDefaultObjectSpaceProvider(CreateCustomObjectSpaceProviderEventArgs args){
            args.ObjectSpaceProvider=new SecuredObjectSpaceProvider((ISelectDataSecurityProvider) Security, new ConnectionStringDataStoreProvider(args.ConnectionString), true);
            args.ObjectSpaceProviders.Add(new NonPersistentObjectSpaceProvider(TypesInfo, null));
            // args.ObjectSpaceProvider = new XPObjectSpaceProvider(new ConnectionStringDataStoreProvider(ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString), true);
        }
    }

}