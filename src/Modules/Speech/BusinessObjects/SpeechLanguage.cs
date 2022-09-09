using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;
using DevExpress.Xpo;
using Xpand.Extensions.XAF.Attributes;
using Xpand.XAF.Persistent.BaseImpl;

namespace Xpand.XAF.Modules.Speech.BusinessObjects {
    [DeferredDeletion(false)][ImageName(("Language"))][CreatableItem(false)]
    [OptimisticLocking(OptimisticLockingBehavior.LockModified)]
    [DefaultProperty(nameof(Name))]
    [SuppressMessage("Design", "XAF0023:Do not implement IObjectSpaceLink in the XPO types")]
    public class SpeechLanguage:CustomBaseObject {
        public SpeechLanguage(Session session) : base(session) { }
        string _name;
        [Association("SpeechToText-TargetLanguagess")][InvisibleInAllViews]
        public XPCollection<SpeechToText> TargetSpeechToTexts => GetCollection<SpeechToText>();
        [RuleRequiredField][RuleUniqueValue]
        public string Name {
            get => _name;
            set => SetPropertyValue(nameof(Name), ref _name, value);
        }

        
    }
}