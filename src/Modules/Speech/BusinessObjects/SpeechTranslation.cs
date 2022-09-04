using System.ComponentModel;
using DevExpress.Persistent.Base;
using DevExpress.Xpo;
using Xpand.XAF.Modules.CloneModelView;

namespace Xpand.XAF.Modules.Speech.BusinessObjects {
    [DefaultProperty(nameof(Text))][DeferredDeletion(false)][OptimisticLocking(OptimisticLockingBehavior.LockModified)]
    [ImageName("Action_ChooseSkin")][CreatableItem(false)]
    [CloneModelView(CloneViewType.ListView, nameof(SpeechTranslation)+"_Banded_ListView")]
    public class SpeechTranslation:SpeechText {
        public SpeechTranslation(Session session) : base(session) { }
        SpeechLanguage _language;

        string _sessionId;

        [Browsable(false)][NonPersistent]
        public string SessionId {
            get => _sessionId;
            set => SetPropertyValue(nameof(SessionId), ref _sessionId, value);
        }
        
        public SpeechLanguage Language {
            get => _language;
            set => SetPropertyValue(nameof(Language), ref _language, value);
        }
        
        SpeechText _sourceText;
        [Association("SpeechText-Translations")]
        public SpeechText SourceText {
            get => _sourceText;
            set => SetPropertyValue(nameof(SourceText), ref _sourceText, value);
        }

        
    }
}