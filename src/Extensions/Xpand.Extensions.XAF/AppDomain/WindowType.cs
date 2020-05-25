using System.Linq;

namespace Xpand.Extensions.XAF.AppDomain{
    public static partial class AppDomainExtensions{
        private static System.Type _webWindowType;
        public static System.Type WebWindowType(this IXAFAppDomain xafAppDomain) => _webWindowType ??= xafAppDomain
            .DXWebAssembly()
            ?.GetTypes().First(type => type.FullName == "DevExpress.ExpressApp.Web.WebWindow");
    }
}