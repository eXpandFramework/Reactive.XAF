using System.IO;
using Fasterflect;
using JetBrains.Annotations;

namespace Xpand.Extensions.AppDomain{
    [PublicAPI]
    public static partial class AppDomainExtensions{
        public static string ApplicationPath(this global::System.AppDomain appDomain){
            if (System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription.StartsWith(".NET Framework")){
                var setupInformation = System.AppDomain.CurrentDomain.GetPropertyValue("SetupInformation");
                return (string) (setupInformation.GetPropertyValue("PrivateBinPath")??setupInformation.GetPropertyValue("ApplicationBase"));
            }
            return appDomain.BaseDirectory;

        }
    }
}