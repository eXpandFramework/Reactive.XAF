using System.Diagnostics.CodeAnalysis;
using DevExpress.Persistent.Base;
using DevExpress.Xpo;
using Xpand.Extensions.XAF.Xpo.BaseObjects;

// ReSharper disable once CheckNamespace
namespace Xpand.XAF.Persistent.BaseImpl{
    [NonPersistent]
    public abstract class CustomBaseObject : XPCustomBaseObject {
                
        [Persistent(nameof(Oid)), Key(true), VisibleInListView(false), VisibleInDetailView(false), VisibleInLookupListView(false), MemberDesignTimeVisibility(false)]
        private long _oid=0 ;
        [PersistentAlias(nameof(_oid)), VisibleInListView(false), VisibleInDetailView(false), VisibleInLookupListView(false)]
        [SuppressMessage("ReSharper", "ConvertToAutoProperty")]
        public long Oid => _oid;
        
        protected CustomBaseObject(Session session):base(session){
        }


    }

}
