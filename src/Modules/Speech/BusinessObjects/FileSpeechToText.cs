using System.ComponentModel;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;
using DevExpress.Xpo;
using Xpand.Extensions.XAF.Xpo.BaseObjects;

namespace Xpand.XAF.Modules.Speech.BusinessObjects {
    [NavigationItem("Speech")][DefaultClassOptions]
    [DeferredDeletion(false)][DefaultProperty(nameof(Name))]
    [ImageName(("Action_Change_State"))]
    [OptimisticLocking(OptimisticLockingBehavior.LockModified)]
    [FileAttachment(nameof(File))]
    public class FileSpeechToText:SpeechToText {
        public FileSpeechToText(Session session) : base(session) { }
        
        FileLinkObject _file;
        [RuleRequiredField]
        [FileTypeFilter("Audio files", 1, "*.wav")]
        public FileLinkObject File {
            get => _file;
            set => SetPropertyValue(nameof(File), ref _file, value);
        }

        protected override bool GetIsValid() => File != null;
    }
}