using System;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Security;
using DevExpress.Persistent.Base;
using Fasterflect;
using JetBrains.Annotations;
using Xpand.Extensions.AppDomainExtensions;

namespace Xpand.Extensions.XAF.SecurityExtensions {
    [UsedImplicitly]
    public static partial class SecurityExtensions {
        private static readonly Type PermissionSettingHelperType;

        static SecurityExtensions() {
            PermissionSettingHelperType = AppDomain.CurrentDomain.GetAssemblyType("DevExpress.ExpressApp.Security.PermissionSettingHelper");
        }

        [PublicAPI]
        public static IPermissionPolicyRole GetDefaultRole(this IObjectSpace objectSpace) => objectSpace.GetDefaultRole("Default");

        public static IPermissionPolicyRole GetDefaultRole(this IObjectSpace objectSpace, string roleName) {
            var defaultRole = objectSpace.GetRole(roleName);
            if (objectSpace.IsNewObject(defaultRole)) {
                defaultRole.AddObjectPermission(SecuritySystem.UserType,SecurityOperations.ReadOnlyAccess, "[Oid] = CurrentUserId()", SecurityPermissionState.Allow);
                defaultRole.AddNavigationPermission(@"Application/NavigationItems/Items/Default/Items/MyDetails", SecurityPermissionState.Allow);
                defaultRole.AddMemberPermission(SecuritySystem.UserType,SecurityOperations.ReadWriteAccess, "ChangePasswordOnFirstLogon; StoredPassword", null, SecurityPermissionState.Allow);
                defaultRole.AddMemberPermission(SecuritySystem.UserType,SecurityOperations.Write, "StoredPassword", "[Oid] = CurrentUserId()", SecurityPermissionState.Allow);
                defaultRole.AddTypePermissionsRecursively(defaultRole.GetType(),SecurityOperations.Read, SecurityPermissionState.Deny);
                var modelDifferenceType = AppDomain.CurrentDomain.GetAssemblyType("DevExpress.Persistent.BaseImpl.ModelDifference");
                var modelDifferenceAspectType = AppDomain.CurrentDomain.GetAssemblyType("DevExpress.Persistent.BaseImpl.ModelDifferenceAspect");
                defaultRole.AddTypePermissionsRecursively(modelDifferenceType,SecurityOperations.Read, SecurityPermissionState.Deny);
                defaultRole.AddTypePermissionsRecursively(modelDifferenceAspectType,SecurityOperations.Read, SecurityPermissionState.Deny);
                defaultRole.AddTypePermissionsRecursively(modelDifferenceType,SecurityOperations.Create, SecurityPermissionState.Allow);
                defaultRole.AddTypePermissionsRecursively(modelDifferenceAspectType,SecurityOperations.Create, SecurityPermissionState.Allow);
            }
            return defaultRole;
        }

        static void AddTypePermissionsRecursively(this IPermissionPolicyRole role, Type targetType, string operations, SecurityPermissionState? state)
            => PermissionSettingHelperType
                .Method(nameof(AddTypePermissionsRecursively), new[] {typeof(IPermissionPolicyRole), typeof(Type), typeof(string), typeof(SecurityPermissionState)}, Flags.StaticPublic)
                .Call(null,role, targetType, operations, state);

        static IPermissionPolicyMemberPermissionsObject AddMemberPermission(this IPermissionPolicyRole role,
            Type type, string operations, string members, string criteria, SecurityPermissionState? state) 
            => (IPermissionPolicyMemberPermissionsObject) PermissionSettingHelperType
                .Method(nameof(AddMemberPermission),
                    new[] {typeof(IPermissionPolicyRole), typeof(Type), typeof(string), typeof(string), typeof(string), typeof(SecurityPermissionState)
                    }, Flags.StaticPublic).Call(null,role, type, operations, members, criteria, state);

        static IPermissionPolicyNavigationPermissionObject AddNavigationPermission(
            this IPermissionPolicyRole role, string itemPath, SecurityPermissionState? state) 
            => (IPermissionPolicyNavigationPermissionObject) PermissionSettingHelperType.Method(nameof(AddNavigationPermission),
                    new []{typeof(IPermissionPolicyRole),typeof(string),typeof(SecurityPermissionState)},Flags.StaticPublic)
                .Call(null,role, itemPath, state);

        static IPermissionPolicyObjectPermissionsObject AddObjectPermission(this IPermissionPolicyRole role, Type type, string operations, string criteria, SecurityPermissionState? state)
            => (IPermissionPolicyObjectPermissionsObject) PermissionSettingHelperType
                .Method(nameof(AddObjectPermission), new[] {typeof(IPermissionPolicyRole), typeof(Type), typeof(string), typeof(string), typeof(SecurityPermissionState)}, Flags.StaticPublic)
                .Call(null, role, type, operations, criteria, state);
        
        public static IPermissionPolicyRole GetRole(this IObjectSpace objectSpace, string roleName) {
            var roleType =(Type)SecuritySystem.Instance.GetPropertyValue("RoleType");
            var securityDemoRole = (IPermissionPolicyRole) (objectSpace.FindObject(roleType, new BinaryOperator("Name", roleName))??objectSpace.CreateObject(roleType));
            securityDemoRole.SetPropertyValue("Name", roleName);
            return securityDemoRole;
        }


    }
}