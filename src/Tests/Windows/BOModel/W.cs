using DevExpress.Persistent.Base;
using DevExpress.Xpo;
using Xpand.XAF.Persistent.BaseImpl;

namespace Xpand.XAF.Modules.Windows.Tests.BOModel{
    [DefaultClassOptions]
    public class W:CustomBaseObject{
        public W(Session session) : base(session){
        }


    }
}