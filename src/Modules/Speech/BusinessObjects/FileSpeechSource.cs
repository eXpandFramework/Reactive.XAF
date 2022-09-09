using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;
using DevExpress.Xpo;
using Xpand.Extensions.XAF.Xpo.BaseObjects;

namespace Xpand.XAF.Modules.Speech.BusinessObjects {
    [OptimisticLocking(OptimisticLockingBehavior.LockModified)]
    [DeferredDeletion(false)][FileAttachment(nameof(File))]
    [DefaultProperty(nameof(Name))][SuppressMessage("Design", "XAF0023:Do not implement IObjectSpaceLink in the XPO types")]
    public class FileSpeechSource:SpeechSource {
        public FileSpeechSource(Session session) : base(session) { }
        FileLinkObject _file;
        [RuleRequiredField]
        [FileTypeFilter("Audio files", 1, "*.wav")]
        public FileLinkObject File {
            get => _file;
            set => SetPropertyValue(nameof(File), ref _file, value);
        }

        protected override string GetName() => $"File: {File}";

        protected override bool GetIsValid() => _file != null;
    }
}