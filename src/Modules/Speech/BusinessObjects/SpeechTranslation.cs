using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using DevExpress.Persistent.Base;
using DevExpress.Xpo;
using Xpand.XAF.Modules.CloneModelView;

namespace Xpand.XAF.Modules.Speech.BusinessObjects {
    [DefaultProperty(nameof(Text))][DeferredDeletion(false)][OptimisticLocking(OptimisticLockingBehavior.LockModified)]
    [ImageName("Action_ChooseSkin")][CreatableItem(false)]
    [CloneModelView(CloneViewType.ListView, SpeechTranslationBandedListView)]
    [CloneModelView(CloneViewType.ListView,SpeechTranslationEditorListView)]
    [SuppressMessage("Design", "XAF0023:Do not implement IObjectSpaceLink in the XPO types")]
    [CloneModelView(CloneViewType.DetailView,"SpeechTranslation_Editor_DetailView")]
    public class SpeechTranslation:SpeechText {
        public const string SpeechTranslationBandedListView = nameof(SpeechTranslation)+"_Banded_ListView";
        public const string SpeechTranslationEditorListView = nameof(SpeechTranslation)+"_Editor_ListView";
        public SpeechTranslation(Session session) : base(session) { }
        SpeechLanguage _language;

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