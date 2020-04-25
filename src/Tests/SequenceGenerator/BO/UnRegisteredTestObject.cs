using DevExpress.Xpo;
using JetBrains.Annotations;

namespace Xpand.XAF.Modules.SequenceGenerator.Tests.BO{
    [PublicAPI]
    public class UnRegisteredTestObject:UnRegisteredTestObjectBase{
        public UnRegisteredTestObject(Session session) : base(session){
        }
    }
    public class UnRegisteredTestObjectBase:TestObject{
        public UnRegisteredTestObjectBase(Session session) : base(session){
        }
    }
}