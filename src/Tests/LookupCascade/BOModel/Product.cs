using DevExpress.Xpo;
using Xpand.XAF.Persistent.BaseImpl;

namespace Xpand.XAF.Modules.LookupCascade.Tests.BOModel{
    public class Product : CustomBaseObject{
        private string _productName;

        public Product(Session session) : base(session){
        }

        public string ProductName{
            get => _productName;
            set => SetPropertyValue(nameof(ProductName), ref _productName, value);
        }

        int _price;

        public int Price{
            get => _price;
            set => SetPropertyValue(nameof(Price), ref _price, value);
        }
        
        [Association("P-To-C")]
        public XPCollection<Accessory> Accessories => GetCollection<Accessory>(nameof(Accessories));
    }
}