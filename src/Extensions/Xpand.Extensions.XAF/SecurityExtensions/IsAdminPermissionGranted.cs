using DevExpress.ExpressApp.Security;
using Fasterflect;

namespace Xpand.Extensions.XAF.SecurityExtensions {
    public static partial class SecurityExtensions {
        public static bool IsAdminPermissionGranted(this ISecurityStrategyBase strategyBase)
            => (bool) strategyBase.CallMethod("IsGranted", AdministrativePermissionRequest.CreateInstance());
    }
}