using DevExpress.Persistent.BaseImpl;
using DevExpress.Xpo;

namespace DevExpress.XAF.Agnostic.Specifications.Modules.ModelViewInheritance.BOModel{
    public class FileMvi:FileAttachmentBase{
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