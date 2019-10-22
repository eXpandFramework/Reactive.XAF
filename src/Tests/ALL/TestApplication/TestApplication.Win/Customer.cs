using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl;
using DevExpress.Xpo;

namespace TestApplication.Win{
    [DefaultClassOptions]
    public class Customer:Person{
        public Customer(Session session) : base(session){
        }
    }
}