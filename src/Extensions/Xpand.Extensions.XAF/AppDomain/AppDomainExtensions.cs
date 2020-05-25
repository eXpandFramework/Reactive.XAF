using System.Linq;

namespace Xpand.Extensions.XAF.AppDomain{
    public static partial class AppDomainExtensions{
        private static object _errorHandlling;
        private static System.Type _errorHandlingType;
        public static object ErrorHandling(this IXAFAppDomain xafAppDomain){
            _errorHandlingType ??= xafAppDomain.DXWebAssembly()?.GetTypes()
                .First(type => type.FullName == "DevExpress.ExpressApp.Web.ErrorHandling");
            return _errorHandlling ??= _errorHandlingType?.GetProperty("Instance")?.GetValue(null);
        }
    }
}