using System;
using System.ComponentModel;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Xpo;
using Xpand.Extensions.Numeric;
using Xpand.Extensions.XAF.Attributes;
using Xpand.Extensions.XAF.Attributes.Custom;
using Xpand.Extensions.XAF.Attributes.Validation;
using Xpand.XAF.Persistent.BaseImpl;

namespace Xpand.XAF.Modules.Telegram.BusinessObjects{
    [DefaultProperty(nameof(Name))][ImageName(nameof(TelegramBot))]
    public class TelegramBot(Session session) : CustomBaseObject(session){
        
        string _name;

        public string Name{
            get => _name;
            set => SetPropertyValue(nameof(Name), ref _name, value);
        }
        bool _active;

        [ColumnDbDefaultValue("0")]
        public bool Active{
            get => _active;
            set => SetPropertyValue(nameof(Active), ref _active, value);
        }
        
        TimeSpan _updateInterval;

        [TimeSpanNotZero]
        public TimeSpan UpdateInterval{
            get => _updateInterval;
            set => SetPropertyValue(nameof(UpdateInterval), ref _updateInterval, value);
        }
        
        int _lastUpdate;
        

        [InvisibleInAllViews]
        [NumericFormat]
        public int LastUpdate{
            get => _lastUpdate;
            set => SetPropertyValue(nameof(LastUpdate), ref _lastUpdate, value);
        }

        DateTime? _updateTime;
        [DisplayDateAndTime][InvisibleInAllViews()]
        [EditorAlias(EditorAliases.LabelPropertyEditor)]
        public DateTime? UpdateTime{
            get => _updateTime;
            set => SetPropertyValue(ref _updateTime, value);
        }
        
        string _startMessageRegex;

        [Size(-1)][ModelDefault("RowCount","1")]
        public string StartMessageRegex{
            get => _startMessageRegex;
            set => SetPropertyValue(nameof(StartMessageRegex), ref _startMessageRegex, value);
        }

        string _stopMessageRegex;
        string _version;

        [ReadOnlyProperty]
        public string Version{
            get => _version;
            set => SetPropertyValue(nameof(Version), ref _version, value);
        }
        
        [Size(-1)][ModelDefault("RowCount","1")]
        public string StopMessageRegex{
            get => _stopMessageRegex;
            set => SetPropertyValue(nameof(StopMessageRegex), ref _stopMessageRegex, value);
        }
        public override void AfterConstruction(){
            base.AfterConstruction();
            _startMessageRegex = "/start";
            _stopMessageRegex = "/stop";
            _updateInterval = 1.Minutes();
            _version = AssemblyInfoVersion.Version;
            DisableWebPagePreview = true;
            MessageTemplates.Add(new TelegramBotMessageTemplate(Session){
                Name = "Welcome",
                Type = TelegramMessageTemplateType.Start,
                Text = "/support - Reach out\n/help - Display available commands"
            });
            Commands.AddRange([
                new TelegramBotCommand(Session){
                    Name = "help",
                    Description = "Show help."
                },
                new TelegramBotCommand(Session){
                    Name = "support",
                    Description = "Reach out with your comments/requests etc.",
                    Parameters = {  new TelegramBotCommandParameter(Session){
                        Required = true,Name = "Message"
                    }}
                }
            ]);
        }
        
        bool _disableWebPagePreview;

        public bool DisableWebPagePreview{
            get => _disableWebPagePreview;
            set => SetPropertyValue(nameof(DisableWebPagePreview), ref _disableWebPagePreview, value);
        }
        

        [Association("TelegramBot-TelegramChats")][Aggregated][ReadOnlyCollection(allowDelete:true)]
        public XPCollection<TelegramChat> Chats => GetCollection<TelegramChat>();

        [PersistentAlias(nameof(Chats) + "[" +nameof(TelegramChat.Active) + "].Count()")]
        public int ActiveChatCount => (int)EvaluateAlias();
        [PersistentAlias(nameof(Chats) + "[" +nameof(TelegramChat.Active) + "].Count()")]
        public int InActiveChatCount => (int)EvaluateAlias();

        [Association("TelegramBot-TelegramBotMessageTemplates")][Aggregated]
        public XPCollection<TelegramBotMessageTemplate> MessageTemplates => GetCollection<TelegramBotMessageTemplate>();

        [Association("TelegramBot-TelegramBotCommands")][Aggregated]
        public XPCollection<TelegramBotCommand> Commands => GetCollection<TelegramBotCommand>();
        
        string _secret;

        [ModelDefault("IsPassword","True")]
        public string Secret{
            get => _secret;
            set => SetPropertyValue(nameof(Secret), ref _secret, value);
        }
    }
    
    public interface ITelegramBotLink{
        TelegramBot Bot{ get; set; }
    }

}
