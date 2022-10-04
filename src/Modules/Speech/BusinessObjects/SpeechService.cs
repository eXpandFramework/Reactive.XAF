using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
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
    [SuppressMessage("Design", "XAF0023:Do not implement IObjectSpaceLink in the XPO types")]
    public class SpeechService:CustomBaseObject, IService {
        public SpeechService(Session session) : base(session) { }

        [Association("SpeechAccount-SpeechVoices")][Aggregated][CollectionOperationSet(AllowAdd = false)]
        
        public XPCollection<SpeechVoice> Voices => GetCollection<SpeechVoice>();

        [InvisibleInAllViews]
        public List<SpeechLanguage> Languages => Voices.Select(voice => voice.Language).Distinct().ToList();
        
        string _key;

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
        [RuleRequiredField][VisibleInListView(false)]
        public string Region {
            get => _region;
            set => SetPropertyValue(nameof(Region), ref _region, value);
        }
    }
}