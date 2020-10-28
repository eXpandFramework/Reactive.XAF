using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Security;
using Xpand.Extensions.TypeExtensions;

namespace Xpand.Extensions.XAF.SecurityExtensions {
    public static partial class SecurityExtensions {
        public static IEnumerable<ISecurityStrategyBase> WhenSecurityStrategy(
            this ISecurityStrategyBase securityStrategy) 
            => securityStrategy?.GetType().InheritsFrom("DevExpress.ExpressApp.Security.SecurityStrategy") == true
                ? new[] {securityStrategy}
                : Enumerable.Empty<ISecurityStrategyBase>();
    }
}