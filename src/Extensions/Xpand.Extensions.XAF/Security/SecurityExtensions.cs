using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp.Security;
using Fasterflect;
using Xpand.Extensions.Type;

namespace Xpand.Extensions.XAF.Security{
    public static class SecurityExtensions{
        public static IEnumerable<ISecurityStrategyBase> WhenSecurityStrategy(this ISecurityStrategyBase securityStrategy){
            return securityStrategy?.GetType().InheritsFrom("DevExpress.ExpressApp.Security.SecurityStrategy") == true ? new[]{securityStrategy}
                : Enumerable.Empty<ISecurityStrategyBase>();
        }

        public static IEnumerable<ISecurityStrategyBase> AddAnonymousType(this ISecurityStrategyBase securityStrategy,params System.Type[] types){
            foreach (var strategyBase in securityStrategy.WhenSecurityStrategy()){
                var anonymousAllowedTypes = strategyBase.GetPropertyValue("AnonymousAllowedTypes");
                foreach (var type in types){
                    anonymousAllowedTypes.CallMethod("Add",type);    
                }
                yield return strategyBase;
            }
        }

    }
}
