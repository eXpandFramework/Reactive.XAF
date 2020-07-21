using DevExpress.Xpo;
using Xpand.XAF.Modules.CloneModelView;
using Xpand.XAF.Persistent.BaseImpl;

namespace Xpand.XAF.Modules.ViewWizard.Tests.BO{
    [CloneModelView(CloneViewType.DetailView, "VW_Child_DetailView1")]
    [CloneModelView(CloneViewType.DetailView, "VW_Child_DetailView2")]
    public class VW:CustomBaseObject{
        public VW(Session session) : base(session){
        }
    }
}
