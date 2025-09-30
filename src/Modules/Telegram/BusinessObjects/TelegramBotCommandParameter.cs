using System.ComponentModel;
using DevExpress.Persistent.Validation;
using DevExpress.Xpo;
using Xpand.XAF.Persistent.BaseImpl;

namespace Xpand.XAF.Modules.Telegram.BusinessObjects{
    [DefaultProperty(nameof(Name))]
    public class TelegramBotCommandParameter(Session session) : CustomBaseObject(session){
        
        string _name;
        [RuleRequiredField]
        public string Name{
            get => _name;
            set => SetPropertyValue(nameof(Name), ref _name, value);
        }

        bool _required;

        public bool Required{
            get => _required;
            set => SetPropertyValue(nameof(Required), ref _required, value);
        }
        
        TelegramBotCommand _telegramBotCommand;

        [Association("TelegramBotCommand-TelegramBotCommandParameters")][RuleRequiredField]
        public TelegramBotCommand TelegramBotCommand{
            get => _telegramBotCommand;
            set => SetPropertyValue(nameof(TelegramBotCommand), ref _telegramBotCommand, value);
        }
    }
}