using System.Linq;
using DevExpress.ExpressApp.Security;
using Fasterflect;

namespace Xpand.Extensions.XAF.SecurityExtensions {
    public static partial class SecurityExtensions {
        public static bool IsActionPermissionGranted(this ISecurityStrategyBase strategyBase, params string[] actions)
            => actions.ToList().All(action =>
                (bool) strategyBase.CallMethod("IsGranted", ActionPermissionRequest.CreateInstance(action)));
    }
}