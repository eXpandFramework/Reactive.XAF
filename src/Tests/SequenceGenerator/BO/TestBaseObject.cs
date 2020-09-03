using DevExpress.Xpo;

namespace Xpand.XAF.Modules.SequenceGenerator.Tests.BO{
    [NonPersistent]
    public abstract class TestBaseObject:XPCustomObject{
        protected TestBaseObject(Session session):base(session){
            
        }

        int _oid;
        [Key(true)]
        public int Oid{
            get => _oid;
            set => SetPropertyValue(nameof(Oid), ref _oid, value);
        }
    }
}