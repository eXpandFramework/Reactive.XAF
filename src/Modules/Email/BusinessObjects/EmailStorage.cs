using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using DevExpress.ExpressApp;
using DevExpress.Xpo;

namespace Xpand.XAF.Modules.Email.BusinessObjects{
    [OptimisticLocking(false)][DeferredDeletion(false)]
    [SuppressMessage("Design", "XAF0023:Do not implement IObjectSpaceLink in the XPO types")]
    public class EmailStorage : XPCustomObject, IObjectSpaceLink{
        public EmailStorage(Session session) : base(session){
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

        string _viewRecipient;

        [Size(255)]
        public string ViewRecipient {
            get => _viewRecipient;
            set => SetPropertyValue(nameof(ViewRecipient), ref _viewRecipient, value);
        }

        string _key;
        [Size(255)]
        public string Key {
            get => _key;
            set => SetPropertyValue(nameof(Key), ref _key, value);
        }
    }
}