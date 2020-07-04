using System;
using System.Runtime.InteropServices;
using Fasterflect;
using JetBrains.Annotations;

namespace Xpand.Extensions.AppDomainExtensions{
    [PublicAPI]
    public static partial class AppDomainExtensions{
	    public static string ApplicationPath(this AppDomain appDomain){
            if (RuntimeInformation.FrameworkDescription.StartsWith(".NET Framework")){
                var setupInformation = AppDomain.CurrentDomain.GetPropertyValue("SetupInformation");
                return (string) (setupInformation.GetPropertyValue("PrivateBinPath")??setupInformation.GetPropertyValue("ApplicationBase"));
            }
            return appDomain.BaseDirectory;

        }
    }
}