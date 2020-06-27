using DevExpress.Xpo;
using Xpand.Extensions.Office.Cloud.BusinessObjects;

namespace Xpand.Extensions.Office.Cloud{
    [DeferredDeletion(false)]
    public class CloudOfficeObject : CloudOfficeBaseObject{
        public CloudOfficeObject(Session session) : base(session){
        }


        string _localId;
        CloudObjectType _cloudObjectType;

        public CloudObjectType CloudObjectType{
            get => _cloudObjectType;
            set => SetPropertyValue(nameof(CloudObjectType), ref _cloudObjectType, value);
        }

        [Indexed(nameof(CloudObjectType), Unique = true)]
        public string LocalId{
            get => _localId;
            set => SetPropertyValue(nameof(LocalId), ref _localId, value);
        }

        string _cloudId;

        [Size(255)]
        public string CloudId{
            get => _cloudId;
            set => SetPropertyValue(nameof(CloudId), ref _cloudId, value);

        }
    }
}