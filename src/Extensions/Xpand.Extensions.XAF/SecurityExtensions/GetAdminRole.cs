using System;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.Persistent.Base;
using Fasterflect;

namespace Xpand.Extensions.XAF.SecurityExtensions {
    public static partial class SecurityExtensions {
        public static IPermissionPolicyRole GetAdminRole(this IObjectSpace objectSpace, string roleName) {
            var roleType = (Type) SecuritySystem.Instance.GetPropertyValue("RoleType");
            var administratorRole =
                (IPermissionPolicyRole) objectSpace.FindObject(roleType, new BinaryOperator("Name", roleName));
            if (administratorRole == null) {
                administratorRole = (IPermissionPolicyRole) objectSpace.CreateObject(roleType);
                administratorRole.Name = roleName;
                administratorRole.IsAdministrative = true;
            }

            return administratorRole;
        }
    }
}