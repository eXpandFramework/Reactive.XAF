using System;
using Fasterflect;


namespace Xpand.Extensions.AppDomainExtensions{
    
    public static partial class AppDomainExtensions{
        public static string ApplicationPath(this AppDomain appDomain){
            if (appDomain.UseNetFramework()){
                var setupInformation = AppDomain.CurrentDomain.GetPropertyValue("SetupInformation");
                return (string) (setupInformation.GetPropertyValue("PrivateBinPath")??setupInformation.GetPropertyValue("ApplicationBase"));
            }
            return appDomain.BaseDirectory;

        }
    }
}