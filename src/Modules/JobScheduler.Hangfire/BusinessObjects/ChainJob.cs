using DevExpress.Persistent.Validation;
using DevExpress.Xpo;
using Xpand.XAF.Persistent.BaseImpl;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire.BusinessObjects {
    public class ChainJob:CustomBaseObject {
        public ChainJob(Session session) : base(session) { }
        int _index;

        Job _owner;

        [Association("Job-ChainJobs")]
        public Job Owner {
            get => _owner;
            set => SetPropertyValue(nameof(Owner), ref _owner, value);
        }

        Job _job;

        [RuleRequiredField]
        public Job Job {
            get => _job;
            set => SetPropertyValue(nameof(Job), ref _job, value);
        }
        
        public int Index {
            get => _index;
            set => SetPropertyValue(nameof(Index), ref _index, value);
        }
    }
}