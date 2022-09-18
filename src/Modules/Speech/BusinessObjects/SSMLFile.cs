using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;
using DevExpress.Xpo;
using Xpand.Extensions.XAF.Attributes.Custom;
using Xpand.Extensions.XAF.Xpo.BaseObjects;
using Xpand.Extensions.XAF.Xpo.ValueConverters;
using Xpand.XAF.Persistent.BaseImpl;

namespace Xpand.XAF.Modules.Speech.BusinessObjects {
    
    [FileAttachment(nameof(File))]
    [DeferredDeletion(false)][DefaultProperty(nameof(File))]
    [ImageName(("Action_Change_State"))][SuppressMessage("Design", "XAF0023:Do not implement IObjectSpaceLink in the XPO types")]
    [OptimisticLocking(OptimisticLockingBehavior.LockModified)]
    public class SSMLFile:CustomBaseObject,ISelectInExplorer {
        public SSMLFile(Session session) : base(session) { }

        [Association("SpeechText-SSMLFiles")]
        public XPCollection<SpeechText> SpeechTexts => GetCollection<SpeechText>();
        FileLinkObject _file;
        
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
        [ValueConverter(typeof(TimeSpanTicksValueConverter))]
        [DisplayDateAndTime(DisplayDateType.None,DisplayTimeType.mm_ss_fff)]
        public TimeSpan Duration {
            get => _duration;
            set => SetPropertyValue(nameof(Duration), ref _duration, value);
        }
    }
}