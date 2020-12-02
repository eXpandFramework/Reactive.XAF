using System.ComponentModel;
using DevExpress.ExpressApp;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl;
using DevExpress.Xpo;

namespace Xpand.TestsLib.Common.BO{
    [FriendlyKeyProperty(nameof(ProductName))]
    public class Product : BaseObject,IObjectSpaceLink{
        public Product(Session session) : base(session){
        }

        string _productName;

        public string ProductName{
            get => _productName;
            set => SetPropertyValue(nameof(ProductName), ref _productName, value);
        }

        [Association("P-To-C")]
        public XPCollection<Accessory> Accessories => GetCollection<Accessory>(nameof(Accessories));
        [Browsable(false)][NonPersistent]

        public IObjectSpace ObjectSpace{ get; set; }
    }
}