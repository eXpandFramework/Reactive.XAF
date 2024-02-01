using System;
using System.ComponentModel;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base.General;
using DevExpress.Xpo;
using Xpand.Extensions.XAF.Attributes;
using Xpand.XAF.Persistent.BaseImpl;

namespace Xpand.Extensions.XAF.Xpo.BaseObjects {
    [DefaultProperty(nameof(NotificationMessage))]
    public class ErrorEvent(Session session) : CustomBaseObject(session), ISupportNotifications {
        string _notificationMessage;

        [Size(SizeAttribute.Unlimited)][XafDisplayName("Msg")]
        public string NotificationMessage{
            get => _notificationMessage;
            set => SetPropertyValue(nameof(NotificationMessage), ref _notificationMessage, value);
        }

        bool _isPostponed;
        [InvisibleInAllViews]
        public bool IsPostponed{
            get => _isPostponed;
            set => SetPropertyValue(nameof(IsPostponed), ref _isPostponed, value);
        }
        [InvisibleInAllViews]
        public object UniqueId => Oid;

        DateTime? _alarmTime;
        [InvisibleInAllViews]
        public DateTime? AlarmTime{
            get => _alarmTime;
            set => SetPropertyValue(nameof(AlarmTime), ref _alarmTime, value);
        }
    }
}