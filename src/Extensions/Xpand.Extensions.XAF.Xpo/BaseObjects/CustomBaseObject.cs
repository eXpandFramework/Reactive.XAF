using System;
using DevExpress.Persistent.Base;
using DevExpress.Xpo;
using Xpand.Extensions.XAF.Xpo.BaseObjects;

// ReSharper disable once CheckNamespace
namespace Xpand.XAF.Persistent.BaseImpl{
    [NonPersistent]
    public abstract class CustomBaseObject : XPCustomBaseObject {
                
        [Persistent("Oid"), Key(true), VisibleInListView(false), VisibleInDetailView(false), VisibleInLookupListView(false), MemberDesignTimeVisibility(false)]
        private Guid _oid = Guid.Empty;
        [PersistentAlias("oid"), VisibleInListView(false), VisibleInDetailView(false), VisibleInLookupListView(false)]
        public Guid Oid => _oid;
        
        protected CustomBaseObject(Session session):base(session){
        }

        public override void AfterConstruction(){
            base.AfterConstruction();
            _oid = XpoDefault.NewGuid();
        }

        protected override void OnSaving() {
            base.OnSaving();
            if (Session is NestedUnitOfWork || !Session.IsNewObject(this) || !_oid.Equals(Guid.Empty))
                return;
            _oid = XpoDefault.NewGuid();
        }
    }

}
