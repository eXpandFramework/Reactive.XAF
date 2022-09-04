using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;
using DevExpress.Xpo;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.XAF.Attributes;
using Xpand.Extensions.XAF.Xpo.BaseObjects;
using Xpand.XAF.Persistent.BaseImpl;

namespace Xpand.XAF.Modules.Speech.BusinessObjects {
    [NavigationItem("Speech")][DefaultClassOptions]
    [FileAttachment(nameof(File))]
    [DeferredDeletion(false)][DefaultProperty(nameof(Name))]
    [ImageName(("Action_Change_State"))]
    [OptimisticLocking(OptimisticLockingBehavior.LockModified)]
    public class SpeechToText:CustomBaseObject {
        public SpeechToText(Session session) : base(session) { }

        [Association("SpeechToText-SpeechTexts")][Aggregated][InvisibleInAllViews]
        public XPCollection<SpeechText> Texts => GetCollection<SpeechText>();

        [SuppressMessage("ReSharper", "CollectionNeverQueried.Global")]
        public BindingList<SSML> TranslationSSMLs { get; } = new();
        [SuppressMessage("ReSharper", "CollectionNeverQueried.Global")][CollectionOperationSet(AllowAdd = false,AllowRemove = false)]
        public BindingList<SpeechTextInfo> SpeechInfo { get; } = new();
        [CollectionOperationSet(AllowAdd = false,AllowRemove = false)]
        public BindingList<SSMLFile> SSMLFiles => Texts.SelectMany(text => text.SSMLFiles).ToBindingList();

        SSML _ssml;
        [NonPersistent]
        public SSML SSML {
            get => _ssml;
            set => SetPropertyValue(nameof(SSML), ref _ssml, value);
        }

        [CollectionOperationSet(AllowAdd = false)][ReloadWhenChange()]
        public BindingList<SpeechText> SpeechTexts => Texts.ExactType().ToBindingList();
        SpeechAccount _speechAccount;

        public override void AfterConstruction() {
            base.AfterConstruction();
            RecognitionLanguage=Session.Query<SpeechLanguage>().FirstOrDefault(language => language.Name == "en-US");
        }


        [Association("SpeechToText-TargetLanguagess")]
        [SuppressMessage("ReSharper", "StringLiteralTypo")]
        public XPCollection<SpeechLanguage> TargetLanguages => GetCollection<SpeechLanguage>();

        [Association("SpeechToText-SpeechVoices")]
        [DataSourceProperty(nameof(AvailableVoices))]
        public XPCollection<SpeechVoice> SpeechVoices => GetCollection<SpeechVoice>();

        public List<SpeechVoice> AvailableVoices => ObjectSpace.GetObjectsQuery<SpeechVoice>().ToArray().Where(
            speechVoice => speechVoice.Account.Oid == Account.Oid && TargetLanguages.Contains(speechVoice.Language)).ToList();
        
        
        [Association("SpeechServiceAccount-SpeechToTexts")]
        public SpeechAccount Account {
            get => _speechAccount;
            set => SetPropertyValue(nameof(SpeechAccount), ref _speechAccount, value);
        }

        FileLinkObject _file;
        [RuleRequiredField]
        [FileTypeFilter("Audio files", 1, "*.wav")]
        public FileLinkObject File {
            get => _file;
            set => SetPropertyValue(nameof(File), ref _file, value);
        }

        string _name;

        [RuleRequiredField][RuleUniqueValue]
        public string Name {
            get => _name;
            set => SetPropertyValue(nameof(Name), ref _name, value);
        }

        SpeechLanguage _recognitionLanguage;
        [RuleRequiredField][DataSourceProperty(nameof(Account)+"."+nameof(SpeechAccount.Languages))]
        public SpeechLanguage RecognitionLanguage {
            get => _recognitionLanguage;
            set => SetPropertyValue(nameof(RecognitionLanguage), ref _recognitionLanguage, value);
        }
        
        
    }
}