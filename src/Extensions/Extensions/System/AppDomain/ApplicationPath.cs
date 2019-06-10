namespace Xpand.Source.Extensions.System.AppDomain{
    internal static partial class AppDomainExtensions{
        public static string ApplicationPath(this global::System.AppDomain appDomain){
            var setupInformation = appDomain.SetupInformation;
            return setupInformation.PrivateBinPath??setupInformation.ApplicationBase;
        }
    }
}