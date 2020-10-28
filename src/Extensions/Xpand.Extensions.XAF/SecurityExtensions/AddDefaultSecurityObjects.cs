using System;
using System.Collections.Generic;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.Updating;
using DevExpress.Persistent.Base;
using Fasterflect;

namespace Xpand.Extensions.XAF.SecurityExtensions {
    public static partial class SecurityExtensions {
        public static void AddDefaultSecurityObjects(this ModuleUpdater updater,Func<ISecurityUserWithRoles,IEnumerable<IPermissionPolicyRole>> roles=null) {
            var objectSpace = (IObjectSpace)updater.GetPropertyValue("ObjectSpace");
            var sampleUser = objectSpace.GetUser("User");
            var defaultRole = objectSpace.GetDefaultRole();
            roles ??= (withRoles => (IEnumerable<IPermissionPolicyRole>) withRoles.GetPropertyValue("Roles")); 
            roles(sampleUser).CallMethod("Add", defaultRole);

            var userAdmin = objectSpace.GetUser("Admin");
            roles(userAdmin).CallMethod("Add", objectSpace.GetAdminRole("Administrators"));
        }
    }
}