using System;
using System.ComponentModel;
using System.Web;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Web;
using DevExpress.ExpressApp.Web.Editors.ASPx;
using DevExpress.ExpressApp.Xpo;
using DevExpress.Persistent.Base;
using Xpand.XAF.Modules.Reactive.Services;

namespace TestApplication.Web{
    public class TestWebApplication : WebApplication{

        public TestWebApplication(){
            ((ISupportInitialize) this).BeginInit();
            Modules.Add(new WebModule());
            ((ISupportInitialize) this).EndInit();
            this.AlwaysUpdateOnDatabaseVersionMismatch().Subscribe();
            ApplicationName = "TestWebApplication";
            CheckCompatibilityType = CheckCompatibilityType.DatabaseSchema;
            InitializeDefaults();
            
        }

        protected override void CreateDefaultObjectSpaceProvider(CreateCustomObjectSpaceProviderEventArgs args){
            var xpoDataStoreProvider = GetDataStoreProvider(args);
            args.ObjectSpaceProvider = new XPObjectSpaceProvider(xpoDataStoreProvider, true);
        }

        private IXpoDataStoreProvider GetDataStoreProvider(CreateCustomObjectSpaceProviderEventArgs args){
            var application = HttpContext.Current != null ? HttpContext.Current.Application : null;
            IXpoDataStoreProvider dataStoreProvider;
            if (application?["DataStoreProvider"] != null){
                dataStoreProvider = application["DataStoreProvider"] as IXpoDataStoreProvider;
            }
            else{
                dataStoreProvider = new MemoryDataStoreProvider();
                // dataStoreProvider = new ConnectionStringDataStoreProvider(ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString);
                if (application != null) application["DataStoreProvider"] = dataStoreProvider;
            }

            return dataStoreProvider;
        }

        static TestWebApplication(){
            EnableMultipleBrowserTabsSupport = true;
            ASPxGridListEditor.AllowFilterControlHierarchy = true;
            ASPxGridListEditor.MaxFilterControlHierarchyDepth = 3;
            ASPxCriteriaPropertyEditor.AllowFilterControlHierarchyDefault = true;
            ASPxCriteriaPropertyEditor.MaxHierarchyDepthDefault = 3;
            PasswordCryptographer.EnableRfc2898 = true;
            PasswordCryptographer.SupportLegacySha512 = false;
        }

        private void InitializeDefaults(){
            LinkNewObjectToParentImmediately = false;
            OptimizedControllersCreation = true;
        }

        
    }
}