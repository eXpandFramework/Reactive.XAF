using System;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.Updating;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl;
using DevExpress.Persistent.BaseImpl.PermissionPolicy;
using Fasterflect;

namespace Xpand.TestsLib{
    public class DefaultUserModuleUpdater : ModuleUpdater{
        private readonly bool _fixedUserId;

        public DefaultUserModuleUpdater(IObjectSpace objectSpace, Version currentDBVersion, bool fixedUserId) : base(objectSpace,
            currentDBVersion){
            _fixedUserId = fixedUserId;
        }

        private PermissionPolicyRole CreateDefaultRole(){
            var defaultRole = ObjectSpace.FindObject<PermissionPolicyRole>(new BinaryOperator("Name", "Default"));
            if (defaultRole == null){
                defaultRole = ObjectSpace.CreateObject<PermissionPolicyRole>();
                defaultRole.Name = "Default";

                defaultRole.AddObjectPermission<PermissionPolicyUser>(SecurityOperations.Read,
                    "[Oid] = CurrentUserId()", SecurityPermissionState.Allow);
                defaultRole.AddNavigationPermission(@"Application/NavigationItems/Items/Default/Items/MyDetails",
                    SecurityPermissionState.Allow);
                defaultRole.AddMemberPermission<PermissionPolicyUser>(SecurityOperations.Write,
                    "ChangePasswordOnFirstLogon", "[Oid] = CurrentUserId()", SecurityPermissionState.Allow);
                defaultRole.AddMemberPermission<PermissionPolicyUser>(SecurityOperations.Write, "StoredPassword",
                    "[Oid] = CurrentUserId()", SecurityPermissionState.Allow);
                defaultRole.AddTypePermissionsRecursively<PermissionPolicyRole>(SecurityOperations.Read,
                    SecurityPermissionState.Deny);
                defaultRole.AddTypePermissionsRecursively<ModelDifference>(SecurityOperations.ReadWriteAccess,
                    SecurityPermissionState.Allow);
                defaultRole.AddTypePermissionsRecursively<ModelDifferenceAspect>(SecurityOperations.ReadWriteAccess,
                    SecurityPermissionState.Allow);
                defaultRole.AddTypePermissionsRecursively<ModelDifference>(SecurityOperations.Create,
                    SecurityPermissionState.Allow);
                defaultRole.AddTypePermissionsRecursively<ModelDifferenceAspect>(SecurityOperations.Create,
                    SecurityPermissionState.Allow);
            }

            return defaultRole;
        }

        public override void UpdateDatabaseAfterUpdateSchema(){
            base.UpdateDatabaseAfterUpdateSchema();
            var sampleUser = ObjectSpace.FindObject<PermissionPolicyUser>(new BinaryOperator("UserName", "User"));
            if (sampleUser == null){
                sampleUser = ObjectSpace.CreateObject<PermissionPolicyUser>();
                sampleUser.UserName = "User";
                sampleUser.SetPassword("");
                if (_fixedUserId){
                    sampleUser.SetMemberValue("oid", Guid.Parse("5c50f5c6-e697-4e9e-ac1b-969eac1237f3"));
                }
            }

            var defaultRole = CreateDefaultRole();
            sampleUser.Roles.Add(defaultRole);

            var userAdmin = ObjectSpace.FindObject<PermissionPolicyUser>(new BinaryOperator("UserName", "Admin"));
            if (userAdmin == null){
                userAdmin = ObjectSpace.CreateObject<PermissionPolicyUser>();
                userAdmin.UserName = "Admin";
                userAdmin.SetPassword("");
            }

            var adminRole = ObjectSpace.FindObject<PermissionPolicyRole>(new BinaryOperator("Name", "Administrators"));
            if (adminRole == null){
                adminRole = ObjectSpace.CreateObject<PermissionPolicyRole>();
                adminRole.Name = "Administrators";
            }

            adminRole.IsAdministrative = true;
            userAdmin.Roles.Add(adminRole);
            ObjectSpace.CommitChanges();
        }
    }
}