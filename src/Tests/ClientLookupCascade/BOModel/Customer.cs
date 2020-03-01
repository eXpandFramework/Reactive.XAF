using DevExpress.Xpo;
using Xpand.XAF.Persistent.BaseImpl;

namespace Xpand.XAF.Modules.ClientLookupCascade.Tests.BOModel{
    public class Customer:CustomBaseObject{
        public Customer(Session session) : base(session){
            
        }

        string _customerName;

        public string CustomerName{
            get => _customerName;
            set => SetPropertyValue(nameof(CustomerName), ref _customerName, value);
        }
    }
}