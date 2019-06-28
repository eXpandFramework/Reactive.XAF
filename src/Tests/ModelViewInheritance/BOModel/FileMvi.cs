using DevExpress.Xpo;
using Xpand.XAF.Persistent.BaseImpl;

namespace Xpand.XAF.Modules.ModelViewInheritance.Tests.BOModel{
    public class FileMvi:CustomBaseObject{
        public FileMvi(Session session) : base(session){
        }

        AMvi _aMvi;

        [Association("AMvi-FileMvis")]
        public AMvi AMvi{
            get => _aMvi;
            set => SetPropertyValue(nameof(AMvi), ref _aMvi, value);
        }
    }
}