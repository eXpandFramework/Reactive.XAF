using System;
using System.ComponentModel;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;
using DevExpress.Xpo;
using Hangfire;
using Xpand.Extensions.XAF.Xpo;
using Xpand.XAF.Modules.Reactive.Attributes;
using Xpand.XAF.Persistent.BaseImpl;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire.BusinessObjects {
    [DefaultProperty(nameof(Id))]
    [DefaultClassOptions][NavigationItem("JobScheduler")]
    public class Job:CustomBaseObject {
        public Job(Session session) : base(session) {
        }

        JobExpression _jobExpression;
        [RuleRequiredField]
        public JobExpression JobExpression {
            get => _jobExpression;
            set => SetPropertyValue(nameof(JobExpression), ref _jobExpression, value);
        }

        public override void AfterConstruction() {
            base.AfterConstruction();
            _cronExpression = Session.GetObjectByKey<CronExpression>(nameof(Cron.Minutely));
        }

        public int Total => SuccessFullJobs + FailedJobs;
        
        CronExpression _cronExpression;
        [RuleRequiredField]
        public CronExpression CronExpression {
            get => _cronExpression;
            set => SetPropertyValue(nameof(CronExpression), ref _cronExpression, value);
        }

        [PersistentAlias(nameof(LastShooter)+"."+nameof(Shooter.State))]
        public ScheduledJobState? State => (ScheduledJobState?) EvaluateAlias();

        [Association("Job-Jobs")][CollectionOperationSet(AllowAdd = false,AllowRemove = false)]
        public XPCollection<Shooter> Jobs => GetCollection<Shooter>(nameof(Jobs));
        
        [PersistentAlias(nameof(FirstShooter)+"."+nameof(Shooter.Created))]
        public DateTime? Created => (DateTime?) EvaluateAlias();

        [SingleObject(nameof(Jobs),nameof(Shooter.Created))][InvisibleInAllViews]
        public Shooter LastShooter => EvaluateAlias() as Shooter;

        [SingleObject(nameof(Jobs),nameof(Shooter.Created),Aggregate.Min)][InvisibleInAllViews]
        public Shooter FirstShooter => (Shooter) EvaluateAlias();

        [PersistentAlias(nameof(LastShooter) + "." + nameof(Shooter.LastState) + "." +nameof(JobState.Created))]
        public DateTime? LastExecution => (DateTime?) EvaluateAlias();

        [PersistentAlias(nameof(Jobs) + ".Sum(" +nameof(Shooter.ExecutionsCount)+ ")")]
        public int? Executions => (int?) EvaluateAlias();

        [PersistentAlias(nameof(Jobs) + "[" + nameof(Shooter.State)+ "='" +nameof(ScheduledJobState.Succeeded)+ "'].Count")]
        public int SuccessFullJobs => (int) EvaluateAlias();
        
        [PersistentAlias(nameof(Jobs) + "[" + nameof(Shooter.State)+ "='" +nameof(ScheduledJobState.Failed)+ "'].Count")]
        public int FailedJobs => (int) EvaluateAlias();

        string _id;
        [RuleUniqueValue][RuleRequiredField][VisibleInListView(true)]
        public string Id {
            get => _id;
            set => SetPropertyValue(nameof(Id), ref _id, value);
        }
    }

    public enum ScheduledJobState {
        Scheduled,
        Enqueued,
        Processing,
        Succeeded,
        Failed,
        Awaiting,
        Deleted
    }
}