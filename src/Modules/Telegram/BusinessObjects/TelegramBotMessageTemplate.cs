using System.ComponentModel;
using DevExpress.Persistent.Validation;
using DevExpress.Xpo;
using Xpand.XAF.Persistent.BaseImpl;

namespace Xpand.XAF.Modules.Telegram.BusinessObjects{
    [DefaultProperty(nameof(Name))]
    public class TelegramBotMessageTemplate(Session session) : CustomBaseObject(session){
        TelegramBot _bot;

        [Association("TelegramBot-TelegramBotMessageTemplates")][RuleRequiredField]
        public TelegramBot Bot{
            get => _bot;
            set => SetPropertyValue(nameof(Bot), ref _bot, value);
        }

        string _text;

        [Size(SizeAttribute.Unlimited)][RuleRequiredField]
        public string Text{
            get => _text;
            set => SetPropertyValue(nameof(Text), ref _text, value);
        }

        string _name;

        public string Name{
            get => _name;
            set => SetPropertyValue(nameof(Name), ref _name, value);
        }

        TelegramMessageTemplateType _type;

        public TelegramMessageTemplateType Type{
            get => _type;
            set => SetPropertyValue(nameof(Type), ref _type, value);
        }
        
    }
    
    public enum TelegramMessageTemplateType{
        Default,Start,Stop
    }

}