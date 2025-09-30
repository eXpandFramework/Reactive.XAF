using System.ComponentModel;
using DevExpress.Persistent.Validation;
using DevExpress.Xpo;
using Xpand.XAF.Persistent.BaseImpl;

namespace Xpand.XAF.Modules.Telegram.BusinessObjects{
    [DefaultProperty(nameof(Name))]
    public class TelegramBotCommand(Session session) : CustomBaseObject(session){
        
        string _name;
        [RuleRequiredField]
        public string Name{
            get => _name;
            set => SetPropertyValue(nameof(Name), ref _name, value);
        }

        string _description;

        [Size(-1)][RuleRequiredField()]
        public string Description{
            get => _description;
            set => SetPropertyValue(nameof(Description), ref _description, value);
        }
        TelegramBot _bot;

        [Association("TelegramBot-TelegramBotCommands")]
        public TelegramBot Bot{
            get => _bot;
            set => SetPropertyValue(nameof(Bot), ref _bot, value);
        }

        [Association("TelegramBotCommand-TelegramBotCommandParameters")][Aggregated]
        public XPCollection<TelegramBotCommandParameter> Parameters => GetCollection<TelegramBotCommandParameter>();
    }
}