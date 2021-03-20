using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp.Security;
using Fasterflect;
using Xpand.Extensions.ObjectExtensions;

namespace Xpand.Extensions.XAF.SecurityExtensions {
    public static partial class SecurityExtensions {
        public static ISecurityStrategyBase[] AddAnonymousType(this ISecurityStrategyBase securityStrategy, params Type[] types) 
            => securityStrategy.AddAnonymousTypeCore(types).ToArray();

        static IEnumerable<ISecurityStrategyBase> AddAnonymousTypeCore(
            this ISecurityStrategyBase securityStrategy, params Type[] types) {
            foreach (var strategyBase in securityStrategy.WhenSecurityStrategy()) {
                var anonymousAllowedTypes = strategyBase.GetPropertyValue("AnonymousAllowedTypes");
                foreach (var type in types) {
                    anonymousAllowedTypes.CallMethod("Add", type);
                }

                yield return strategyBase;
            }
        }
    }
}