using System;
using System.ComponentModel;
using DevExpress.Xpo;
using Xpand.Extensions.XAF.Xpo.ValueConverters;
using Xpand.XAF.Persistent.BaseImpl;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire.Notification.BusinessObjects {
    
    [DeferredDeletion(false)]
    [DefaultProperty(nameof(ObjectType))]
    public class NotificationJobIndex:CustomBaseObject {
        public NotificationJobIndex(Session session) : base(session) { }
        [Association("NotificationJob-NotificationJobIndexs")]
        public ObjectStateNotification ObjectStateNotification {
            get => _objectStateNotification;
            set => SetPropertyValue(nameof(ObjectStateNotification), ref _objectStateNotification, value);
        }
        
        Type _objectType;

        [Size(SizeAttribute.Unlimited)]
        [ValueConverter(typeof(TypeValueConverter))]
        public Type ObjectType {
            get => _objectType;
            set => SetPropertyValue(nameof(ObjectType), ref _objectType, value);
        }

        ObjectStateNotification _objectStateNotification;

        

        long _index;

        public long Index {
            get => _index;
            set => SetPropertyValue(nameof(Index), ref _index, value);
        }
    }
}