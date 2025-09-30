using DevExpress.ExpressApp.DC;
using DevExpress.Xpo;
using Xpand.Extensions.XAF.NonPersistentObjects;

namespace Xpand.XAF.Modules.Telegram.BusinessObjects{
    [DomainComponent]
    public class TelegramMessage:NonPersistentBaseObject{
        string _text;

        [Size(-1)]
        public string Text{
            get => _text;
            set => SetPropertyValue(nameof(Text), ref _text, value);
        }
    }
}