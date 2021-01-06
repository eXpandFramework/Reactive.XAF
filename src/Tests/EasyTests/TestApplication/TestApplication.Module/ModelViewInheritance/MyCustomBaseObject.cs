using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl;
using DevExpress.Xpo;

namespace TestApplication.Module.ModelViewInheritance {
    [DefaultClassOptions]
    public abstract class Customer:Person {
        protected Customer(Session session) : base(session) { }

        
    }
    
    public class Partner:Customer {
        public Partner(Session session) : base(session) { }
    }
}
