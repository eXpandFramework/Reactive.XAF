using System.ComponentModel;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;
using DevExpress.Xpo;
using Xpand.XAF.Persistent.BaseImpl;

namespace Xpand.XAF.Modules.Speech.BusinessObjects {
    [NavigationItem("Speech")][DefaultClassOptions]
    [DefaultProperty(nameof(Name))][DeferredDeletion(false)][ImageName(("BO_User"))]
    [OptimisticLocking(OptimisticLockingBehavior.LockModified)]
    public class TranslatorService:CustomBaseObject {
        public TranslatorService(Session session) : base(session) {
            
        }

        string _name;

        public string Name {
            get => _name;
            set => SetPropertyValue(nameof(Name), ref _name, value);
        }
        
        [ModelDefault("IsPassword","True")]
        [RuleRequiredField][VisibleInListView(false)]
        public string Key {
            get => _key;
            set => SetPropertyValue(nameof(Key), ref _key, value);
        }

        string _region;
        private string _key;

        [RuleRequiredField][VisibleInListView(false)]
        public string Region {
            get => _region;
            set => SetPropertyValue(nameof(Region), ref _region, value);
        }
    }
}