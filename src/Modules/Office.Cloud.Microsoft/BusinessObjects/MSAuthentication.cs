using DevExpress.Xpo;

using Xpand.Extensions.Office.Cloud.BusinessObjects;

namespace Xpand.XAF.Modules.Office.Cloud.Microsoft.BusinessObjects{
	
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