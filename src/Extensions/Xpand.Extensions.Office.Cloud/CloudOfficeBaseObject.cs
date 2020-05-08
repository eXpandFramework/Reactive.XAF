using System;
using System.ComponentModel;
using DevExpress.ExpressApp;
using DevExpress.Xpo;

namespace Xpand.Extensions.Office.Cloud{
    [NonPersistent]
    public abstract class CloudOfficeBaseObject : XPCustomObject, IObjectSpaceLink{
        protected CloudOfficeBaseObject(Session session) : base(session){
        }

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