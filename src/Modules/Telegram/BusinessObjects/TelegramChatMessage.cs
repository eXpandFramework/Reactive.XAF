using System;
using System.ComponentModel;
using DevExpress.Data;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Xpo;
using Xpand.Extensions.DateTimeExtensions;
using Xpand.Extensions.StringExtensions;
using Xpand.Extensions.XAF.Attributes;
using Xpand.Extensions.XAF.Attributes.Custom;
using Xpand.XAF.Modules.CloneModelView;
using Xpand.XAF.Persistent.BaseImpl;

namespace Xpand.XAF.Modules.Telegram.BusinessObjects{
    [ImageName("TelegramChatMessage")][ListViewShowFooter]
    [DefaultProperty(nameof(TelegramChat))]
    [CloneModelView(CloneViewType.DetailView, "TelegramChatMessage_DetailView_Reply")]
    public class TelegramChatMessage(Session session) : CustomBaseObject(session){
        
        protected override void OnSaving(){
            base.OnSaving();
            HasReply = Reply.IsNotNullOrEmpty();
        }

        TelegramChat _telegramChat;

        [Association("TelegramChat-TelegramMessages")]
        public TelegramChat TelegramChat{
            get => _telegramChat;
            set => SetPropertyValue(nameof(TelegramChat), ref _telegramChat, value);
        }
        string _message;

        [Size(-1)][VisibleInAllViews]
        [ColumnSummary(SummaryType.Count)]
        public string Message{
            get => _message;
            set => SetPropertyValue(nameof(Message), ref _message, value);
        }


        DateTime _created;

        [DisplayDateAndTime][ColumnSorting(ColumnSortOrder.Descending)]
        [InvisibleInAllViews]
        public DateTime Created{
            get => _created;
            set => SetPropertyValue(nameof(Created), ref _created, value);
        }

        string _reply;

        [Size(-1)]
        public string Reply{
            get => _reply;
            set => SetPropertyValue(nameof(Reply), ref _reply, value);
        }
        
        [SortProperty(nameof(Created))][DisplayName(nameof(Created))]
        public string CreatedString => Created.HumanizeCompact();

        bool _hasReply;

        [InvisibleInAllViews]
        [ColumnDbDefaultValue("0")]
        public bool HasReply{
            get => _hasReply;
            set => SetPropertyValue(nameof(HasReply), ref _hasReply, value);
        }
    }
}