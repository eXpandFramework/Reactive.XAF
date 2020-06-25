using DevExpress.Xpo;
using JetBrains.Annotations;

namespace Xpand.Extensions.Office.Cloud.Microsoft{
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