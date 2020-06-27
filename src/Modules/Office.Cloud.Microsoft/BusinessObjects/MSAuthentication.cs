using DevExpress.Xpo;
using JetBrains.Annotations;
using Xpand.Extensions.Office.Cloud.BusinessObjects;

namespace Xpand.XAF.Modules.Office.Cloud.Microsoft.BusinessObjects{
	[UsedImplicitly]
    public class MSAuthentication : CloudOfficeBaseObject{
        public MSAuthentication(Session session) : base(session){
        }

        byte[] _token;

        public byte[] Token{
            get => _token;
            set => SetPropertyValue(nameof(Token), ref _token, value);
        }
    }
}