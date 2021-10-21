using System;
using System.IO;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Xpo;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Xpand.Extensions.AppDomainExtensions;

namespace TestApplication.Module.Common {
    public static class Extensions{
        public static void ConfigureConnectionString(this XafApplication application){
            application.ConnectionString = InMemoryDataStoreProvider.ConnectionString;
            var easyTestSettingsFile = AppDomain.CurrentDomain.EasyTestSettingsFile();
            if (File.Exists(easyTestSettingsFile)){
                var settings = JsonConvert.DeserializeObject<dynamic>(File.ReadAllText(easyTestSettingsFile));
                application.ConnectionString = settings?.ConnectionString;
            }
        }

        public static string EasyTestSettingsFile(this AppDomain appDomain) 
            =>appDomain.IsHosted()? $"{appDomain.ApplicationPath()}\\..\\EasyTestSettings.json":$"{appDomain.ApplicationPath()}\\EasyTestSettings.json";
    }
}