using System;
using System.ComponentModel;
using DevExpress.ExpressApp;
using DevExpress.Xpo;

namespace Xpand.Extensions.Office.Cloud.BusinessObjects{
    [NonPersistent][OptimisticLocking(false)][DeferredDeletion(false)]
    public abstract class CloudOfficeBaseObject(Session session) : XPCustomObject(session), IObjectSpaceLink {
        protected override void OnSaving(){
            base.OnSaving();
            if (Session is NestedUnitOfWork || !Session.IsNewObject(this) || !Oid.Equals(Guid.Empty))
                return;
            Oid = XpoDefault.NewGuid();
        }

        [Browsable(false)]
        public IObjectSpace ObjectSpace { get; set; }
        Guid _oid;
        [Key]
        public Guid Oid{
            get => _oid;
            set => SetPropertyValue(nameof(Oid), ref _oid, value);
        }
    }
}