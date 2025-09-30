using System.ComponentModel;
using System.Linq;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;
using DevExpress.Xpo;
using Xpand.Extensions.XAF.Attributes;
using Xpand.Extensions.XAF.Xpo.BaseObjects;

namespace Xpand.XAF.Modules.Telegram.BusinessObjects{
    [ImageName("TelegramUser")][ListViewShowFooter]
    [DefaultProperty(nameof(DisplayName))]
    public class TelegramUser(Session session) : XPCustomBaseObject(session){
        [Association("TelegramChat-TelegramUsers")][ReadOnlyCollection(allowNew:false)]
        public XPCollection<TelegramChat> Chats => GetCollection<TelegramChat>();

        [NonPersistent]
        public string DisplayName {
            get {
                if (!string.IsNullOrWhiteSpace(UserName)) return $"@{UserName}";
                var fullName = string.Join(" ", new[] { FirstName, LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));
                return !string.IsNullOrWhiteSpace(fullName) ? fullName : $"User: {Id}";
            }
        }


        long _id;

        [Key]
        public long Id{
            get => _id;
            set => SetPropertyValue(nameof(Id), ref _id, value);
        }

        string _userName;

        [RuleUniqueValue(SkipNullOrEmptyValues = true)]
        public string UserName{
            get => _userName;
            set => SetPropertyValue(nameof(UserName), ref _userName, value);
        }

        string _lastName;
        public string LastName{
            get => _lastName;
            set => SetPropertyValue(nameof(LastName), ref _lastName, value);
        }

        string _firstName;

        public string FirstName{
            get => _firstName;
            set => SetPropertyValue(nameof(FirstName), ref _firstName, value);
        }
    }
}