using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;
using DevExpress.Xpo;
using Xpand.Extensions.XAF.SecurityExtensions;

namespace Xpand.TestsLib.Common {
    public class DefaultUserModuleUpdater : ModuleUpdater {
        private readonly Guid _userId;
        private readonly bool _admin;

        public DefaultUserModuleUpdater(IObjectSpace objectSpace1, Version versionFromDB, Guid userId, bool admin) : base(objectSpace1, versionFromDB) {
            _userId = userId;
            _admin = admin;
        }

        public override void UpdateDatabaseAfterUpdateSchema() {
            base.UpdateDatabaseAfterUpdateSchema();
            this.AddDefaultSecurityObjects();
            if (_userId!=Guid.Empty){
                ((XPBaseObject) ObjectSpace.GetUser(_admin?"Admin":"User")).SetMemberValue("oid", _userId);
            }
            ObjectSpace.CommitChanges();
        }
    }
}