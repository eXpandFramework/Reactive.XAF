using System;
using System.ComponentModel;
using DevExpress.Xpo;
using Xpand.Extensions.XAF.Attributes;
using Xpand.Extensions.XAF.Xpo;
using Xpand.Extensions.XAF.Xpo.BaseObjects;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire.BusinessObjects {
    [DefaultProperty(nameof(Id))]
    public class JobWorker:XPCustomBaseObject {
        public JobWorker(Session session) : base(session) {
        }

        string _id;

        [Key]
        public string Id {
            get => _id;
            set => SetPropertyValue(nameof(Id), ref _id, value);
        }
        public ScheduledJobState? State => LastState?.State;

        [PersistentAlias(nameof(LastState) + "." + nameof(JobState.Created))]
        public DateTime Created => (DateTime) EvaluateAlias();

        [PersistentAlias(nameof(Executions) + "[" + nameof(JobState.State)+ "='" +nameof(ScheduledJobState.Processing)+ "'].Count")]
        public int ExecutionsCount=>(int) EvaluateAlias();

        [SingleObject(nameof(Executions),nameof(JobState.Created))][InvisibleInAllViews]
        public JobState LastState => (JobState) EvaluateAlias();

        Job _job;

        [Association("Job-Jobs")]
        public Job Job {
            get => _job;
            set => SetPropertyValue(nameof(Job), ref _job, value);
        }
        [Association("JobWorker-JobStates")]
        public XPCollection<JobState> Executions => GetCollection<JobState>(nameof(Executions));
    }
}