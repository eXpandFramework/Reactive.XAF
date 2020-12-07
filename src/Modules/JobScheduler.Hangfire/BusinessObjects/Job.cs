using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;
using DevExpress.Xpo;
using Fasterflect;
using Hangfire;
using Xpand.Extensions.XAF.Attributes;
using Xpand.Extensions.XAF.NonPersistentObjects;
using Xpand.Extensions.XAF.ObjectExtensions;
using Xpand.Extensions.XAF.Xpo;
using Xpand.Extensions.XAF.Xpo.ValueConverters;
using Xpand.XAF.Persistent.BaseImpl;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire.BusinessObjects {
    [DefaultProperty(nameof(Id))]
    [DefaultClassOptions][NavigationItem("JobScheduler")]
    public class  Job:CustomBaseObject {
        public Job(Session session) : base(session) {
        }
        ObjectType _jobType;

        [RuleRequiredField]
        [DataSourceProperty(nameof(JobTypes))][Size(SizeAttribute.Unlimited)]
        [ValueConverter(typeof(ObjectTypeValueConverter))]
        [Persistent]
        public ObjectType JobType {
            get => _jobType;
            set {
                SetPropertyValue(nameof(JobType), ref _jobType, value);
                OnChanged(nameof(JobMethod));
            }
        }

        ObjectString _jobMethod;
        [ValueConverter(typeof(ObjectStringValueConverter))]
        [Persistent][DataSourceProperty(nameof(JobMethods))][Size(SizeAttribute.Unlimited)]
        [RuleRequiredField]
        public ObjectString JobMethod {
            get => _jobMethod;
            set => SetPropertyValue(nameof(JobMethod), ref _jobMethod, value);
        }

        [Browsable(false)]
        public IList<ObjectString> JobMethods 
            => AppDomain.CurrentDomain.JobMethods().Where(info => info.DeclaringType==JobType?.Type)
                .Select(info => new ObjectString(info.Attribute<JobProviderAttribute>().DisplayName??info.Name.CompoundName() ))
                .ToArray();
        
        [Browsable(false)]
        public IList<ObjectType> JobTypes => AppDomain.CurrentDomain.JobMethods().Select(m=>m.DeclaringType).Distinct()
            .Select(type => new ObjectType(type){Name = type.Attribute<JobProviderAttribute>().DisplayName??type.Name.CompoundName()})
            .ToArray();

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

        [PersistentAlias(nameof(LastJobWorker)+"."+nameof(JobWorker.State))]
        public ScheduledJobState? State => (ScheduledJobState?) EvaluateAlias();

        [Association("Job-Jobs")][CollectionOperationSet(AllowAdd = false,AllowRemove = false)]
        public XPCollection<JobWorker> Jobs => GetCollection<JobWorker>(nameof(Jobs));
        
        [PersistentAlias(nameof(FirstJobWorker)+"."+nameof(JobWorker.Created))]
        public DateTime? Created => (DateTime?) EvaluateAlias();

        [SingleObject(nameof(Jobs),nameof(JobWorker.Created))][InvisibleInAllViews]
        public JobWorker LastJobWorker => EvaluateAlias() as JobWorker;

        [SingleObject(nameof(Jobs),nameof(JobWorker.Created),Aggregate.Min)][InvisibleInAllViews]
        public JobWorker FirstJobWorker => (JobWorker) EvaluateAlias();

        [PersistentAlias(nameof(LastJobWorker) + "." + nameof(JobWorker.LastState) + "." +nameof(JobState.Created))]
        public DateTime? LastExecution => (DateTime?) EvaluateAlias();

        [PersistentAlias(nameof(Jobs) + ".Sum(" +nameof(JobWorker.ExecutionsCount)+ ")")]
        public int? Executions => (int?) EvaluateAlias();

        [PersistentAlias(nameof(Jobs) + "[" + nameof(JobWorker.State)+ "='" +nameof(ScheduledJobState.Succeeded)+ "'].Count")]
        public int SuccessFullJobs => (int) EvaluateAlias();
        
        [PersistentAlias(nameof(Jobs) + "[" + nameof(JobWorker.State)+ "='" +nameof(ScheduledJobState.Failed)+ "'].Count")]
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