using System;
using System.Reflection;
using Fasterflect;

namespace Xpand.Extensions.AppDomainExtensions{
    public static partial class AppDomainExtensions{
        public static string ApplicationName(this AppDomain appDomain){
            if (appDomain.UseNetFramework()){
                var setupInformation = AppDomain.CurrentDomain.GetPropertyValue("SetupInformation");
                return (string) (setupInformation.GetPropertyValue("ApplicationName")) ;
            }

            return Assembly.GetEntryAssembly()?.GetName().Name??"ApplicationName";
        }
    }
}