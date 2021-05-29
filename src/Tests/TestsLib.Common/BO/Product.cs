using DevExpress.Persistent.Base;
using DevExpress.Xpo;
using Xpand.XAF.Persistent.BaseImpl;

namespace Xpand.TestsLib.Common.BO{
    [FriendlyKeyProperty(nameof(ProductName))][DefaultClassOptions]
    public class Product : CustomBaseObject{
        public Product(Session session) : base(session){
        }

        string _productName;

        public string ProductName{
            get => _productName;
            set => SetPropertyValue(nameof(ProductName), ref _productName, value);
        }

        int _id;

        public int Id {
            get => _id;
            set => SetPropertyValue(nameof(Id), ref _id, value);
        }

        [Association("P-To-Instance")]
        public XPCollection<Accessory> Accessories => GetCollection<Accessory>(nameof(Accessories));
        

        
    }
}