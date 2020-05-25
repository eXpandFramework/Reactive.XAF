using System.Linq;

namespace Xpand.Extensions.XAF.AppDomain{
    public static partial class AppDomainExtensions{
        private static System.Reflection.Assembly _dxWebAssembly;

        public static System.Reflection.Assembly DXWebAssembly(this IXAFAppDomain xafAppDomain) => _dxWebAssembly ??= 
            xafAppDomain.AppDomain.GetAssemblies().FirstOrDefault(assembly => assembly.GetName().Name.StartsWith("DevExpress.ExpressApp.Web.v"));
    }
}