using System;
using System.Diagnostics.CodeAnalysis;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;
using DevExpress.Xpo;
using Xpand.Extensions.XAF.Attributes.Custom;
using Xpand.Extensions.XAF.Xpo.BaseObjects;
using Xpand.XAF.Persistent.BaseImpl;

namespace Xpand.XAF.Modules.Speech.BusinessObjects {
    [NavigationItem("Speech")][DefaultClassOptions][OptimisticLocking(OptimisticLockingBehavior.LockModified)]
    [FileAttachment(nameof(File))][DeferredDeletion(false)][ImageName("XafBarLinkContainerItem")]
    [SuppressMessage("Design", "XAF0023:Do not implement IObjectSpaceLink in the XPO types")]
    public class TextToSpeech:CustomBaseObject {
        public TextToSpeech(Session session) : base(session) { }
        FileLinkObject _file;
        [RuleRequiredField]
        [FileTypeFilter("Audio files", 1, "*.wav")]
        public FileLinkObject File {
            get => _file;
            set => SetPropertyValue(nameof(File), ref _file, value);
        }

        string _text;

        [Size(-1)]
        public string Text {
            get => _text;
            set => SetPropertyValue(nameof(Text), ref _text, value);
        }

        TimeSpan _duration;
        [DisplayDateAndTime(DisplayDateType.None,DisplayTimeType.mm_ss)]
        public TimeSpan Duration {
            get => _duration;
            set => SetPropertyValue(nameof(Duration), ref _duration, value);
        }
    }
}