using DevExpress.Persistent.BaseImpl.PermissionPolicy;
using DevExpress.Xpo;

namespace Xpand.XAF.Modules.Email.Tests.BOModel {
    public class EmailUser:PermissionPolicyUser {
        public EmailUser(Session session) : base(session) { }
        string _email;
        public override void AfterConstruction() {
            base.AfterConstruction();
            Email = "apostolis.bekiaris@gmail.com";
        }

        public string Email {
            get => _email;
            set => SetPropertyValue(nameof(Email), ref _email, value);
        }
    }
}