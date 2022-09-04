using System;
using System.ComponentModel;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;
using DevExpress.Xpo;
using Xpand.Extensions.XAF.Xpo.BaseObjects;
using Xpand.Extensions.XAF.Xpo.ValueConverters;
using Xpand.XAF.Persistent.BaseImpl;

namespace Xpand.XAF.Modules.Speech.BusinessObjects {
    
    [FileAttachment(nameof(File))]
    [DeferredDeletion(false)][DefaultProperty(nameof(File))]
    [ImageName(("Action_Change_State"))]
    [OptimisticLocking(OptimisticLockingBehavior.LockModified)]
    public class SSMLFile:CustomBaseObject,ISelectInExplorer {
        public SSMLFile(Session session) : base(session) { }

        [Association("SpeechText-SSMLFiles")]
        public XPCollection<SpeechText> SpeechTexts => GetCollection<SpeechText>();
        FileLinkObject _file;
        [RuleRequiredField]
        [FileTypeFilter("Audio files", 1, "*.wav")]
        public FileLinkObject File {
            get => _file;
            set => SetPropertyValue(nameof(File), ref _file, value);
        }

        SpeechLanguage _language;

        public SpeechLanguage Language {
            get => _language;
            set => SetPropertyValue(nameof(Language), ref _language, value);
        }

        TimeSpan _duration;
        [ValueConverter(typeof(TimeSpanSecondsValueConverter))]
        public TimeSpan Duration {
            get => _duration;
            set => SetPropertyValue(nameof(Duration), ref _duration, value);
        }
    }
}