using System;
using System.Runtime.InteropServices;

namespace Xpand.Extensions.AppDomainExtensions{
    public static partial class AppDomainExtensions{
        public static bool UseNetFramework(this AppDomain appDomain) 
            => RuntimeInformation.FrameworkDescription.StartsWith(".NET Framework");
    }
}