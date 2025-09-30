using System.ComponentModel;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Xpo;
using Xpand.Extensions.XAF.Attributes;
using Xpand.Extensions.XAF.Xpo.BaseObjects;

namespace Xpand.XAF.Modules.Telegram.BusinessObjects{
    [ImageName("TelegramChat")][OptimisticLocking(OptimisticLockingBehavior.NoLocking)]
    [ListViewShowFooter]
    [DefaultProperty(nameof(User))]
    public class TelegramChat(Session session) : XPCustomBaseObject(session){
        [Association("TelegramChat-TelegramMessages")][Aggregated][ReadOnlyCollection]
        public XPCollection<TelegramChatMessage> Messages => GetCollection<TelegramChatMessage>();

        
        bool _active;
        
        public bool Active{
            get => _active;
            set => SetPropertyValue(nameof(Active), ref _active, value);
        }
        TelegramBot _bot;

        [Association("TelegramBot-TelegramChats")]
        public TelegramBot Bot{
            get => _bot;
            set => SetPropertyValue(nameof(Bot), ref _bot, value);
        }
        
        TelegramUser _user;
        [Association("TelegramChat-TelegramUsers")]
        [ColumnSummary(SummaryType.Count)]
        public TelegramUser User{
            get => _user;
            set => SetPropertyValue(nameof(User), ref _user, value);
        }
        
        long _id;

        [Key]
        public long Id{
            get => _id;
            set => SetPropertyValue(nameof(Id), ref _id, value);
        }    
    }
    
    public interface ITelegramChatLink{
        TelegramChat Chat{ get; set; }
        string Link{ get; }
    }

}