using System;
using DevExpress.Persistent.BaseImpl.PermissionPolicy;
using DevExpress.Xpo;

namespace Xpand.XAF.Modules.Reactive.Rest.Tests.BO {
    public class RestUser : PermissionPolicyUser, ICredentialBearer {
        public RestUser(Session session) : base(session) { }
        public string BaseAddress { get; set; } = "http://www.expandframework.com/api";
        public string Key { get; set; } = Guid.NewGuid().ToString();
        public string Secret { get; set; } = Guid.NewGuid().ToString();
    }
}