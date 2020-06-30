using DevExpress.ExpressApp;

namespace Xpand.Extensions.XAF.AppDomainExtensions{
    public static partial class AppDomainExtensions{
        
        public static XafApplication WebApplication(this IXAFAppDomain xafAppDomain) =>
	        (XafApplication) xafAppDomain.WebApplicationType()?.GetProperty("Instance")?.GetValue(null);
    }
}