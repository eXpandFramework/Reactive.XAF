using DevExpress.Persistent.Base;
using DevExpress.Xpo;
using Xpand.XAF.Persistent.BaseImpl;

namespace Xpand.XAF.Modules.Reactive.Tests.BOModel{
    [DefaultClassOptions]
    public class R:CustomBaseObject{
        public R(Session session) : base(session){
        }
        
        
    }
}