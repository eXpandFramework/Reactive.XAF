using DevExpress.Xpo;
using Xpand.XAF.Persistent.BaseImpl;

namespace Xpand.XAF.Modules.ClientLookupCascade.Tests.BOModel{
    public class Project:CustomBaseObject{
        public Project(Session session) : base(session){
        }

        Customer _customer;

        
        public Customer Customer{
            get => _customer;
            set => SetPropertyValue(nameof(Customer), ref _customer, value);
        }

        Order _order;

        
        public Order Order{
            get => _order;
            set => SetPropertyValue(nameof(Order), ref _order, value);
        }
    }
}