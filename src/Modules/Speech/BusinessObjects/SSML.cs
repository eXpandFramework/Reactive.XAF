using System.Collections.Generic;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Xpo;
using Xpand.Extensions.XAF.NonPersistentObjects;

namespace Xpand.XAF.Modules.Speech.BusinessObjects {
    [DomainComponent][ImageName("OutlookNavigation_Reading")][CreatableItem(false)]
    public class SSML:NonPersistentBaseObject {
        string _text;

        [Size(SizeAttribute.Unlimited)]
        public string Text {
            get => _text;
            set => SetPropertyValue(nameof(Text), ref _text, value);
        }

        SpeechLanguage _language;

        public SpeechLanguage Language {
            get => _language;
            set => SetPropertyValue(nameof(Language), ref _language, value);
        }

        public List<long> SpeechTexts { get; } = new();
    }
}