using DevExpress.ExpressApp.Security;
using Xpand.Extensions.ObjectExtensions;

namespace Xpand.Extensions.XAF.SecurityExtensions {
    public static partial class SecurityExtensions {
        public static bool IsSecurityStrategyComplex(this ISecurityStrategyBase strategyBase) => strategyBase
            .IsInstanceOf("DevExpress.ExpressApp.Security.SecurityStrategyComplex");
    }
}