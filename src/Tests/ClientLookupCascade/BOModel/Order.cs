using DevExpress.Xpo;
using Xpand.XAF.Persistent.BaseImpl;

namespace Xpand.XAF.Modules.ClientLookupCascade.Tests.BOModel{
    public class Order:CustomBaseObject{
        public Order(Session session) : base(session){
        }

        string _orderName;

        public string OrderName{
            get => _orderName;
            set => SetPropertyValue(nameof(OrderName), ref _orderName, value);
        }
    }
}