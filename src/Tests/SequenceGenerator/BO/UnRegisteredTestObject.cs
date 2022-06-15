using DevExpress.Xpo;


namespace Xpand.XAF.Modules.SequenceGenerator.Tests.BO{
    
    public class UnRegisteredTestObject:UnRegisteredTestObjectBase{
        public UnRegisteredTestObject(Session session) : base(session){
        }
    }
    public class UnRegisteredTestObjectBase:TestObject{
        public UnRegisteredTestObjectBase(Session session) : base(session){
        }
    }
}