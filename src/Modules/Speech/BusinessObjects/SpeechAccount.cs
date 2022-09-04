using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;
using DevExpress.Xpo;
using Xpand.Extensions.XAF.Attributes;
using Xpand.XAF.Persistent.BaseImpl;

namespace Xpand.XAF.Modules.Speech.BusinessObjects {
    [NavigationItem("Speech")][DefaultClassOptions]
    [DefaultProperty(nameof(Name))][DeferredDeletion(false)][ImageName(("BO_User"))]
    [OptimisticLocking(OptimisticLockingBehavior.LockModified)]
    public class SpeechAccount:CustomBaseObject {
        public SpeechAccount(Session session) : base(session) { }

        [Association("SpeechAccount-SpeechVoices")][Aggregated][CollectionOperationSet(AllowAdd = false)]
        
        public XPCollection<SpeechVoice> Voices => GetCollection<SpeechVoice>();

        [InvisibleInAllViews]
        public List<SpeechLanguage> Languages => Voices.Select(voice => voice.Language).Distinct().ToList();
        
        [Association("SpeechServiceAccount-SpeechToTexts")][InvisibleInAllViews]
        public XPCollection<SpeechToText> SpeechToTexts => GetCollection<SpeechToText>();
        string _subscription;

        string _name;

        public string Name {
            get => _name;
            set => SetPropertyValue(nameof(Name), ref _name, value);
        }
        
        [ModelDefault("IsPassword","True")]
        [RuleRequiredField][VisibleInListView(false)]
        public string Subscription {
            get => _subscription;
            set => SetPropertyValue(nameof(Subscription), ref _subscription, value);
        }

        string _region;
        [RuleRequiredField][VisibleInListView(false)]
        public string Region {
            get => _region;
            set => SetPropertyValue(nameof(Region), ref _region, value);
        }
    }
}