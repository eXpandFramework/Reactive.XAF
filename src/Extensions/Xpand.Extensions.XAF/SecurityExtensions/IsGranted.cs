using DevExpress.ExpressApp.Security;

namespace Xpand.Extensions.XAF.SecurityExtensions {
    public static partial class SecurityExtensions {
        public static bool IsGranted(this ISecurityStrategyBase security, PermissionRequest permissionRequest)
            => ((IRequestSecurity)security).IsGranted(permissionRequest);
    }
}