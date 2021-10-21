using System.ComponentModel;
using DevExpress.Persistent.Validation;
using DevExpress.Xpo;
using Xpand.XAF.Persistent.BaseImpl;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire.Notification.BusinessObjects.NotificationChannel {
    [DefaultProperty(nameof(Name))]
    public abstract class NotificationChannel:CustomBaseObject {
        protected NotificationChannel(Session session) : base(session) { }

        string _name;

        [Association("NotificationChannel-NotificationJobs")]
        public XPCollection<ObjectStateNotification> NotificationJobs => GetCollection<ObjectStateNotification>(nameof(NotificationJobs));
        
        [RuleRequiredField][RuleUniqueValue]
        public string Name {
            get => _name;
            set => SetPropertyValue(nameof(Name), ref _name, value);
        }
    }
}