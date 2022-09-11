using System.ComponentModel;
using DevExpress.Persistent.Base;
using DevExpress.Xpo;

namespace Xpand.XAF.Modules.Speech.BusinessObjects {
    [NavigationItem("Speech")][DefaultClassOptions]
    [DeferredDeletion(false)][DefaultProperty(nameof(Name))]
    [ImageName(("Action_Change_State"))]
    [OptimisticLocking(OptimisticLockingBehavior.LockModified)]
    public class FileSpeechToText:SpeechToText {
        public FileSpeechToText(Session session) : base(session) { }
    }
}