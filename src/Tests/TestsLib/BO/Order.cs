using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl;
using DevExpress.Xpo;

namespace Xpand.TestsLib.BO{
    [DefaultClassOptions]
    public class Order : BaseObject{
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

        [Association("Order-AggregatedOrders")] [Aggregated]
        public XPCollection<Order> AggregatedOrders => GetCollection<Order>(nameof(AggregatedOrders));

        Order _aggregatedOrder;

        [Association("Order-AggregatedOrders")][VisibleInListView(false)][VisibleInDetailView(false)][VisibleInLookupListView(false)]
        public Order AggregatedOrder{
            get => _aggregatedOrder;
            set => SetPropertyValue(nameof(AggregatedOrder), ref _aggregatedOrder, value);
        }
    }
}