using System.Diagnostics.CodeAnalysis;
using DevExpress.Xpo;

using Xpand.Extensions.Office.Cloud.BusinessObjects;

namespace Xpand.XAF.Modules.Office.Cloud.Microsoft.BusinessObjects{
    [SuppressMessage("Design", "XAF0023:Do not implement IObjectSpaceLink in the XPO types")]
    public class MSAuthentication : CloudOfficeBaseObject{
        public MSAuthentication(Session session) : base(session){
        }

        string _token;
        [Size(SizeAttribute.Unlimited)]
        public string Token{
            get => _token;
            set => SetPropertyValue(nameof(Token), ref _token, value);
        }

        string _userName;

        public string UserName{
	        get => _userName;
	        set => SetPropertyValue(nameof(UserName), ref _userName, value);
        }
    }
}