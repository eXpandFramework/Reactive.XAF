namespace Xpand.Extensions.AppDomain{
    public static partial class AppDomainExtensions{
        public static string ApplicationPath(this global::System.AppDomain appDomain){
            return appDomain.BaseDirectory;
//            var setupInformation = appDomain.BaseDirectory;
//            return setupInformation.PrivateBinPath??setupInformation.ApplicationBase;
        }
    }
}