using DevExpress.Persistent.Base;
using DevExpress.Xpo;
using System.Diagnostics.CodeAnalysis;
using Xpand.XAF.Persistent.BaseImpl;

namespace Xpand.TestsLib.Common.BO{
    [FriendlyKeyProperty(nameof(ProductName))][DefaultClassOptions]
    [SuppressMessage("Design", "XAF0023:Do not implement IObjectSpaceLink in the XPO types")]
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