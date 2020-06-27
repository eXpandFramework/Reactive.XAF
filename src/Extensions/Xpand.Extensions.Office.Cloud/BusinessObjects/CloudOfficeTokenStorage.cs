using DevExpress.Xpo;
using JetBrains.Annotations;

namespace Xpand.Extensions.Office.Cloud.BusinessObjects{
    [DeferredDeletion(false)]
    [UsedImplicitly]
    public class CloudOfficeTokenStorage : CloudOfficeBaseObject, ITokenStore{
        public CloudOfficeTokenStorage(Session session) : base(session){
        }

        string _entityName;
        public string EntityName{
            get => _entityName;
            set => SetPropertyValue(nameof(EntityName), ref _entityName, value);
        }

        string _tokenType;

        public string TokenType{
            get => _tokenType;
            set => SetPropertyValue(nameof(TokenType), ref _tokenType, value);
        }

        string _token;
        [Size(SizeAttribute.Unlimited)]
        public string Token{
            get => _token;
            set => SetPropertyValue(nameof(Token), ref _token, value);
        }
    }

    public interface ITokenStore{
        string TokenType { get; set; }
        string Token { get; set; }
        string EntityName { get; set; }
    }

}