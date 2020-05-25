using System.Linq;

namespace Xpand.Extensions.XAF.AppDomain{
    public static partial class AppDomainExtensions{
        private static System.Type _webApplicationType;
        public static System.Type WebApplicationType(this IXAFAppDomain xafAppDomain) => _webApplicationType ??=
            xafAppDomain.DXWebAssembly()
                ?.GetTypes().First(type => type.FullName == "DevExpress.ExpressApp.Web.WebApplication");
    }
}