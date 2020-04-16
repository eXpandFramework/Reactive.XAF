using DevExpress.Xpo;
using Xpand.XAF.Persistent.BaseImpl;

namespace Xpand.XAF.Modules.LookupCascade.Tests.BOModel{
    public class Order : CustomBaseObject{
        private Accessory _accessory;
        private Product _product;

        public Order(Session session) : base(session){
        }

        

        public Product Product{
            get => _product;
            set => SetPropertyValue(nameof(Product), ref _product, value);
        }

        public Accessory Accessory{
            get => _accessory;
            set => SetPropertyValue(nameof(Accessory), ref _accessory, value);
        }
    }
}