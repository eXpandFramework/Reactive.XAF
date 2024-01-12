using System.ComponentModel;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.Security;
using DevExpress.Persistent.BaseImpl;
using DevExpress.Persistent.BaseImpl.PermissionPolicy;
using DevExpress.Xpo;

namespace TestApplication.Blazor.Server {
    [MapInheritance(MapInheritanceType.ParentTable)]
    [DefaultProperty(nameof(UserName))]
    public class ApplicationUser(Session session)
        : PermissionPolicyUser(session), ISecurityUserWithLoginInfo, ISecurityUserLockout {
        private int _accessFailedCount;
        private DateTime _lockoutEnd;

        [Browsable(false)]
        public int AccessFailedCount {
            get => _accessFailedCount;
            set => SetPropertyValue(nameof(AccessFailedCount), ref _accessFailedCount, value);
        }

        [Browsable(false)]
        public DateTime LockoutEnd {
            get => _lockoutEnd;
            set => SetPropertyValue(nameof(LockoutEnd), ref _lockoutEnd, value);
        }

        [Browsable(false)]
        [Aggregated, Association("User-LoginInfo")]
        public XPCollection<ApplicationUserLoginInfo> LoginInfo => GetCollection<ApplicationUserLoginInfo>();

        IEnumerable<ISecurityUserLoginInfo> IOAuthSecurityUser.UserLogins => LoginInfo;

        ISecurityUserLoginInfo ISecurityUserWithLoginInfo.CreateUserLoginInfo(string loginProviderName, string providerUserKey) {
            ApplicationUserLoginInfo result = new ApplicationUserLoginInfo(Session) {
                LoginProviderName = loginProviderName,
                ProviderUserKey = providerUserKey,
                User = this
            };
            return result;
        }
    }
    [DeferredDeletion(false)]
    [Persistent("PermissionPolicyUserLoginInfo")]
    public class ApplicationUserLoginInfo(Session session) : BaseObject(session), ISecurityUserLoginInfo {
        private string _loginProviderName;
        private ApplicationUser _user;
        private string _providerUserKey;

        [Indexed("ProviderUserKey", Unique = true)]
        [Appearance("PasswordProvider", Enabled = false, Criteria = "!(IsNewObject(this)) and LoginProviderName == '" + SecurityDefaults.PasswordAuthentication + "'", Context = "DetailView")]
        public string LoginProviderName {
            get => _loginProviderName;
            set => SetPropertyValue(nameof(LoginProviderName), ref _loginProviderName, value);
        }

        [Appearance("PasswordProviderUserKey", Enabled = false, Criteria = "!(IsNewObject(this)) and LoginProviderName == '" + SecurityDefaults.PasswordAuthentication + "'", Context = "DetailView")]
        public string ProviderUserKey {
            get => _providerUserKey;
            set => SetPropertyValue(nameof(ProviderUserKey), ref _providerUserKey, value);
        }

        [Association("User-LoginInfo")]
        public ApplicationUser User {
            get => _user;
            set => SetPropertyValue(nameof(User), ref _user, value);
        }

        object ISecurityUserLoginInfo.User => User;
    }

}