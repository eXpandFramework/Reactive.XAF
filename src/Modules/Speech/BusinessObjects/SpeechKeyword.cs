using System.ComponentModel;
using DevExpress.Persistent.Base;
using DevExpress.Xpo;
using Xpand.XAF.Modules.StoreToDisk;
using Xpand.XAF.Persistent.BaseImpl;

namespace Xpand.XAF.Modules.Speech.BusinessObjects {
    [OptimisticLocking(OptimisticLockingBehavior.LockModified)]
    [DeferredDeletion(false)][DefaultProperty(nameof(Name))]
    [NavigationItem("Speech")]
    public class SpeechKeyword:CustomBaseObject {
        public SpeechKeyword(Session session) : base(session) { }

        SpeechLanguage _language;

        public SpeechLanguage Language {
            get => _language;
            set => SetPropertyValue(nameof(Language), ref _language, value);
        }

        string _name;

        public string Name {
            get => _name;
            set => SetPropertyValue(nameof(Name), ref _name, value);
        }
        
        string _text;
        
        [Size(-1)][ToolTip("Use ';' to seperate values")]
        public string Text {
            get => _text;
            set => SetPropertyValue(nameof(Text), ref _text, value);
        }
    }
}