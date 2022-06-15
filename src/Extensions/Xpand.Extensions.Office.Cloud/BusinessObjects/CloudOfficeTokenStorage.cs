using DevExpress.Xpo;


namespace Xpand.Extensions.Office.Cloud.BusinessObjects{
    [DeferredDeletion(false)]
    
    public class CloudOfficeTokenStorage : CloudOfficeBaseObject{
        public CloudOfficeTokenStorage(Session session) : base(session){
        }

        

        [Association("CloudOfficeTokenStorage-CloudOfficeTokens")]
        public XPCollection<CloudOfficeToken> CloudOfficeTokens => GetCollection<CloudOfficeToken>(nameof(CloudOfficeTokens));
    }

    [DeferredDeletion(false)]
    
    public class CloudOfficeToken:CloudOfficeBaseObject,ICloudOfficeToken{
        public CloudOfficeToken(Session session) : base(session){
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

        CloudOfficeTokenStorage _cloudOfficeTokenStorage;

        [Association("CloudOfficeTokenStorage-CloudOfficeTokens")]
        public CloudOfficeTokenStorage CloudOfficeTokenStorage{
            get => _cloudOfficeTokenStorage;
            set => SetPropertyValue(nameof(CloudOfficeTokenStorage), ref _cloudOfficeTokenStorage, value);
        }
    }
    public interface ICloudOfficeToken{
        string TokenType { get; set; }
        string Token { get; set; }
        string EntityName { get; set; }
    }

}