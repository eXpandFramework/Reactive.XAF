using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;
using DevExpress.Xpo;
using Microsoft.CognitiveServices.Speech;
using Xpand.XAF.Persistent.BaseImpl;

namespace Xpand.XAF.Modules.Speech.BusinessObjects {
    [DeferredDeletion(false)] [CreatableItem(false)][OptimisticLocking(OptimisticLockingBehavior.LockModified)]
    [DefaultProperty(nameof(ShortName))][ImageName("BO_Department")]
    [SuppressMessage("Design", "XAF0023:Do not implement IObjectSpaceLink in the XPO types")]
    public class SpeechVoice:CustomBaseObject {
        public SpeechVoice(Session session) : base(session) { }
        string _name;

        [Association("SpeechToText-SpeechVoices")]
        public XPCollection<SpeechToText> SpeechToTexts => GetCollection<SpeechToText>();
        SpeechService _speechService;

        [Association("SpeechAccount-SpeechVoices")][RuleRequiredField]
        public SpeechService Service {
            get => _speechService;
            set => SetPropertyValue(nameof(SpeechService), ref _speechService, value);
        }
        
        [RuleRequiredField]
        public string Name {
            get => _name;
            set => SetPropertyValue(nameof(Name), ref _name, value);
        }

        SynthesisVoiceGender _gender;

        public SynthesisVoiceGender Gender {
            get => _gender;
            set => SetPropertyValue(nameof(Gender), ref _gender, value);
        }

        SpeechLanguage _language;

        public SpeechLanguage Language {
            get => _language;
            set => SetPropertyValue(nameof(Language), ref _language, value);
        }

        SynthesisVoiceType _voiceType;

        public SynthesisVoiceType VoiceType {
            get => _voiceType;
            set => SetPropertyValue(nameof(VoiceType), ref _voiceType, value);
        }

        string _voicePath;

        public string VoicePath {
            get => _voicePath;
            set => SetPropertyValue(nameof(VoicePath), ref _voicePath, value);
        }

        string _shortName;

        public string ShortName {
            get => _shortName;
            set => SetPropertyValue(nameof(ShortName), ref _shortName, value);
        }
    }
}