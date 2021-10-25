using System.ComponentModel;
using DevExpress.Persistent.Base;
using DevExpress.Xpo;
using Xpand.XAF.Persistent.BaseImpl;

namespace Xpand.TestsLib.Common.BO{
    [FriendlyKeyProperty(nameof(AccessoryName))]
    [DefaultProperty(nameof(AccessoryName))]
    public class Accessory : CustomBaseObject{
        private string _accessoryName;
        private long _accessoryID;
        private Product _product;

        public Accessory(Session session) : base(session){
        }

        public string AccessoryName{
            get => _accessoryName;
            set => SetPropertyValue(nameof(AccessoryName), ref _accessoryName, value);
        }

        public long AccesoryID{
            get => _accessoryID;
            set => SetPropertyValue(nameof(AccesoryID), ref _accessoryID, value);
        }

        [Association("P-To-Instance")]
        public Product Product{
            get => _product;
            set => SetPropertyValue(nameof(Product), ref _product, value);
        }
    }
}