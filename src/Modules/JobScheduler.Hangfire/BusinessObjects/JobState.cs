using System;
using System.ComponentModel;
using DevExpress.Xpo;
using Xpand.XAF.Persistent.BaseImpl;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire.BusinessObjects {
    
    [DefaultProperty(nameof(State))]
    public class JobState:CustomBaseObject {
        public JobState(Session session) : base(session) { }

        string _reason;

        [Size(SizeAttribute.Unlimited)]
        public string Reason {
            get => _reason;
            set => SetPropertyValue(nameof(Reason), ref _reason, value);
        }
        
        ScheduledJobState _state;

        public ScheduledJobState State {
            get => _state;
            set => SetPropertyValue(nameof(State), ref _state, value);
        }
        DateTime _created;

        public DateTime Created {
            get => _created;
            set => SetPropertyValue(nameof(Created), ref _created, value);
        }
        
        JobWorker _jobWorker;

        [Association("JobWorker-JobStates")]
        public JobWorker JobWorker {
            get => _jobWorker;
            set => SetPropertyValue(nameof(JobWorker), ref _jobWorker, value);
        }
        
        
    }
}