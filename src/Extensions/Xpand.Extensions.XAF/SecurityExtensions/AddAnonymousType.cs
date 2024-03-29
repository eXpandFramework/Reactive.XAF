﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Security;
using Fasterflect;
using Xpand.Extensions.ObjectExtensions;
using Xpand.Extensions.XAF.ActionExtensions;

namespace Xpand.Extensions.XAF.SecurityExtensions {
    public static partial class SecurityExtensions {
        public static ISecurityStrategyBase[] AddAnonymousType(this ISecurityStrategyBase securityStrategy, params Type[] types) 
            => securityStrategy.AddAnonymousTypeCore(types).ToArray();

        static IEnumerable<ISecurityStrategyBase> AddAnonymousTypeCore(
            this ISecurityStrategyBase securityStrategy, params Type[] types) {
            foreach (var strategyBase in securityStrategy.WhenSecurityStrategy()) {
                var anonymousAllowedTypes = strategyBase.GetPropertyValue("AnonymousAllowedTypes");
                strategyBase.SetPropertyValue("AllowAnonymousAccess", true);
                foreach (var type in types) {
                    anonymousAllowedTypes.CallMethod("Add", type);
                }

                yield return strategyBase;
            }
        }
    }
}