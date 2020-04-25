using DevExpress.Xpo;
using BaseObject = DevExpress.Persistent.BaseImpl.BaseObject;

namespace Xpand.XAF.Modules.SequenceGenerator.Tests.BO{
    [NonPersistent]
    public abstract class TestBaseObject:BaseObject{
        protected TestBaseObject(Session session):base(session){
            
        }
    }
}