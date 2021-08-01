using DevExpress.Xpo;
using Xpand.XAF.Modules.Reactive.Rest;

public class User:DevExpress.Persistent.BaseImpl.PermissionPolicy.PermissionPolicyUser,ICredentialBearer {
	public User(Session session) : base(session) { }
	public string BaseAddress { get; set; }
	public string Key { get; set; }
	public string Secret { get; set; }
}