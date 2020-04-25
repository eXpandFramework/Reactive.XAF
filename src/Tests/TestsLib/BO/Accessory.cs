using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl;
using DevExpress.Xpo;

namespace Xpand.TestsLib.BO{
    [FriendlyKeyProperty(nameof(AccessoryName))]
    public class Accessory : BaseObject{
        private string _accessoryName;
        private long _accesoryID;
        private Product _product;

        public Accessory(Session session) : base(session){
        }

        public string AccessoryName{
            get => _accessoryName;
            set => SetPropertyValue(nameof(AccessoryName), ref _accessoryName, value);
        }

        public long AccesoryID{
            get => _accesoryID;
            set => SetPropertyValue(nameof(AccesoryID), ref _accesoryID, value);
        }

        [Association("P-To-C")]
        public Product Product{
            get => _product;
            set => SetPropertyValue(nameof(Product), ref _product, value);
        }
    }
}