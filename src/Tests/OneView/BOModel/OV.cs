using DevExpress.Persistent.Base;
using DevExpress.Xpo;
using Xpand.XAF.Persistent.BaseImpl;

namespace Xpand.XAF.Modules.OneView.Tests.BOModel{
    public class OV:CustomBaseObject{
        public OV(Session session) : base(session){
        }


    }
    [DefaultClassOptions]
    public class A:CustomBaseObject{
        public A(Session session) : base(session){
        }


    }
}